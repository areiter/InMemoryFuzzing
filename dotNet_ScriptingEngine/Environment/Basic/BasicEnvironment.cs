using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.IO;
using DevEck.ScriptingEngine.Common;

namespace DevEck.ScriptingEngine.Environment.Basic
{
    /// <summary>
    /// Defines a basic script environment
    /// </summary>
    public class BasicEnvironment : IScriptEnvironment, IRuntimeDataProvider
    {
        /// <summary>
        /// The current scripting language
        /// </summary>
        protected ScriptingLanguage _scriptingLanguage;

        /// <summary>
        /// Defines some code generation options that are applied while
        /// generating the assembly
        /// </summary>
        protected BasicCodeGenerationOptions _codeGenerationOptions = new BasicCodeGenerationOptions();

        /// <summary>
        /// Language specific code generator
        /// </summary>
        protected CodeDomProvider _compiler = null;
        
        /// <summary>
        /// Contains objects for the defined parameters
        /// </summary>
        protected Dictionary<ParameterInfo, object> _globalParameterAssignments =
            new Dictionary<ParameterInfo, object>();

        /// <summary>
        /// Contains delegates for the defined methods
        /// </summary>
        protected Dictionary<MethodInfo, Delegate> _globalMethodAssignments =
            new Dictionary<MethodInfo, Delegate>();

        /// <summary>
        /// Additional imports
        /// </summary>
        protected List<string> _extraImports = new List<string>();

        /// <summary>
        /// Contains the Execute() code
        /// </summary>
        protected string _mainCode = "";

        /// <summary>
        /// Contains extra user defined Methods or classes
        /// </summary>
        protected string _extraCode = "";

        /// <summary>
        /// Gibt an ob die Assembly neu generiert werden muss
        /// </summary>
        protected bool _recompile = true;

        /// <summary>
        /// Letzte Assembly die den auszuführenden Code enthält
        /// </summary>
        protected CompilerResults _compilerResults = null;

		public CompilerResults CompilerResults 
		{
			get { return _compilerResults; }
		}
		
        protected CodeDomProvider Compiler
        {
            get
            {
                if (_compiler == null)
                {
                    if (_scriptingLanguage == ScriptingLanguage.VBdotNet)
                        _compiler = new Microsoft.VisualBasic.VBCodeProvider();
                    else if (_scriptingLanguage == ScriptingLanguage.CSharp)
                        _compiler = new Microsoft.CSharp.CSharpCodeProvider();
                    else
                        throw new NotImplementedException(string.Format("Scripting language '{0}' not supported", _scriptingLanguage));
                }

                return _compiler;
            }
        }

        public BasicEnvironment(ScriptingLanguage scriptingLanguage)
        {
            _scriptingLanguage = scriptingLanguage;
        }

        #region IScriptEnvironment Members

        protected List<ParameterInfo> _globalParameters = new List<ParameterInfo>();

        /// <summary>
        /// Returns all globally defined parameters.
        /// </summary>
        public IList<ParameterInfo> GlobalParameters
        {
            get { return _globalParameters; }
        }

        protected List<MethodInfo> _globalMethods = new List<MethodInfo>();

        /// <summary>
        /// Returns all globally defined Methods
        /// </summary>
        public IList<MethodInfo> GlobalMethods
        {
            get { return _globalMethods; }
        }

        /// <summary>
        /// Gets or Sets the Code of the Entry method
        /// </summary>
        public virtual string MainCode 
        {
            get { return _mainCode; }
            set
            {
                _mainCode = value;
                _recompile = true;
            }
        }

        /// <summary>
        /// Gets or Sets ExtraCode
        /// </summary>
        public virtual string ExtraCode 
        {
            get { return _extraCode; }
            set
            {
                _extraCode = value;
                _recompile = true;
            }
        }

        /// <summary>
        /// Sets the given global parameter to the specified value
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <param name="parameter"></param>
        public virtual void SetParameter(ParameterInfo parameterInfo, object parameter)
        {
            if (_globalParameterAssignments.ContainsKey(parameterInfo))
                _globalParameterAssignments[parameterInfo] = parameter;
            else
                _globalParameterAssignments.Add(parameterInfo, parameter);
        }

        /// <summary>
        /// Sets the method to the given delegate
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="method"></param>
        public virtual void SetMethod(MethodInfo methodInfo, Delegate method)
        {
            if (_globalMethodAssignments.ContainsKey(methodInfo))
                _globalMethodAssignments[methodInfo] = method;
            else
                _globalMethodAssignments.Add(methodInfo, method);
        }

        /// <summary>
        /// Generates the code
        /// </summary>
        /// <returns></returns>
        protected virtual CompilerResults GenerateCode()
        {
            if (_recompile == false)
                return _compilerResults;
            else
            {
                List<Assembly> referencedAssemblies = new List<Assembly>();
                foreach (AssemblyName asmName in this.GetType().Assembly.GetReferencedAssemblies())
                {
                    Assembly asm = Assembly.Load(asmName);
                    referencedAssemblies.Add(asm);
                }


                CodeCompileUnit compileUnit = new CodeCompileUnit();

                //Create our namespace statement
                CodeNamespace executionNamespace = new CodeNamespace(_codeGenerationOptions.Namespace);
                compileUnit.Namespaces.Add(executionNamespace);

                //Add some imports
                executionNamespace.Imports.Add(new CodeNamespaceImport("System"));
                executionNamespace.Imports.Add(new CodeNamespaceImport(typeof(IRuntimeDataProvider).Namespace));

                foreach (string nspace in _extraImports)
                    executionNamespace.Imports.Add(new CodeNamespaceImport(nspace));

                referencedAssemblies.Add(typeof(IRuntimeDataProvider).Assembly);


                //Define the execution environment class, and add to the namespace
                CodeTypeDeclaration execEnv = new CodeTypeDeclaration("ExecutionEnvironment");
                executionNamespace.Types.Add(execEnv);

                //Defines the member field for the IRuntimeDataProvider
                CodeHelpers.DefineField(execEnv, CodeHelpers.ModifierEnum.Private,
                    new ParameterInfo("_scriptDataProvider", typeof(IRuntimeDataProvider)));

                //Defines the accessor for the IRuntimeDataProvider
                CodeHelpers.DefineAccessor(execEnv, CodeHelpers.ModifierEnum.Public,
                    new ParameterInfo("ScriptDataProvider", typeof(IRuntimeDataProvider)), true, true,
                    "_scriptDataProvider");

                GenerateProperties(execEnv, referencedAssemblies);

                //Defines the constructor 
                //ctor(IRuntimeDataProvider runtimeDataProvider)
                CodeConstructor ctor = CodeHelpers.DefineConstructor(execEnv, CodeHelpers.ModifierEnum.Public,
                    new ParameterInfo("runtimeDataProvider", typeof(IRuntimeDataProvider)));
                ctor.Statements.Add(
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_scriptDataProvider"),
                        new CodeVariableReferenceExpression("runtimeDataProvider")
                        )
                    );


                //Generates the global methods
                GenerateMethods(execEnv, referencedAssemblies);


                //Define the entry Method
                CodeMemberMethod memberExecute = CodeHelpers.DefineMemberMethod(
                    execEnv, "Execute", CodeHelpers.ModifierEnum.Public, null);

                memberExecute.Statements.Add(new CodeSnippetExpression(_mainCode));
                execEnv.Members.Add(new CodeSnippetTypeMember(_extraCode));

                StringBuilder bld = new StringBuilder();
                CodeGeneratorOptions opt = new CodeGeneratorOptions();
                opt.BracingStyle = "C";
                Compiler.GenerateCodeFromCompileUnit(compileUnit, new StringWriter(bld), opt);

                CompilerParameters compilerParameters = new CompilerParameters();
                compilerParameters.IncludeDebugInformation = true;

                foreach (Assembly refAsm in referencedAssemblies)
                    compilerParameters.ReferencedAssemblies.Add(refAsm.Location);

                _compilerResults = Compiler.CompileAssemblyFromDom(compilerParameters, compileUnit);
                _recompile = false;
                return _compilerResults;
            }
        }

        /// <summary>
        /// Generates the properties for the global Parameters
        /// </summary>
        /// <param name="classDecl"></param>
        protected virtual void GenerateProperties(CodeTypeDeclaration classDecl, IList<Assembly> referencedAssemblies)
        {
            foreach (ParameterInfo parameter in _globalParameters)
            {
                CodeMemberProperty property = CodeHelpers.DefineProperty(classDecl, CodeHelpers.ModifierEnum.Private,
                    parameter, true, false);

                CodeMethodReferenceExpression scriptDataProvider = new CodeMethodReferenceExpression(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(), 
                        "_scriptDataProvider"),
                    "GetData",
                    new CodeTypeReference(parameter.ParameterType)
                    );

                property.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(scriptDataProvider, new CodePrimitiveExpression(parameter.Name))
                    )
                    );


                Assembly asm = parameter.ParameterType.Assembly;
                if (referencedAssemblies.Contains(asm) == false)
                    referencedAssemblies.Add(asm);
            }
        }

        /// <summary>
        /// Generates the global methods
        /// </summary>
        /// <param name="classDecl"></param>
        /// <param name="referencedAssemblies"></param>
        protected virtual void GenerateMethods(CodeTypeDeclaration classDecl, IList<Assembly> referencedAssemblies)
        {
            CodeMethodReferenceExpression scriptDataProvider = new CodeMethodReferenceExpression(
                   new CodeFieldReferenceExpression(
                       new CodeThisReferenceExpression(),
                       "_scriptDataProvider"),
                   "GetMethod"
                   );

            foreach (MethodInfo method in _globalMethods)
            {
                CodeMemberMethod memberMethod = CodeHelpers.DefineMemberMethod(classDecl, method.Name, CodeHelpers.ModifierEnum.Private,
                    new ParameterInfo(null, method.ReturnType), method.Parameters);


                CodeExpression getDelegate = new CodeMethodInvokeExpression(scriptDataProvider, new CodePrimitiveExpression(method.Name));

                List<CodeExpression> dynamicInvokeParameters = new List<CodeExpression>();

                if(method.Parameters != null)
                {
                    foreach (ParameterInfo parameter in method.Parameters)
                    {
                        dynamicInvokeParameters.Add(new CodeVariableReferenceExpression(parameter.Name));

                        Assembly asm = parameter.ParameterType.Assembly;
                        if (referencedAssemblies.Contains(asm) == false)
                            referencedAssemblies.Add(asm);
                    }
                }


                CodeExpression delegateInvoke = new CodeMethodInvokeExpression(
                    getDelegate, "DynamicInvoke", dynamicInvokeParameters.ToArray());

                if (method.ReturnType == null)
                    memberMethod.Statements.Add(delegateInvoke);
                else
                {
                    memberMethod.Statements.Add(new CodeMethodReturnStatement(new CodeCastExpression(new CodeTypeReference(method.ReturnType), delegateInvoke)));

                    Assembly asm = method.ReturnType.Assembly;
                    if (referencedAssemblies.Contains(asm) == false)
                        referencedAssemblies.Add(asm);
                }

            }
        }


        /// <summary>
        /// Executes the script
        /// </summary>
        public virtual void Execute()
        {
            CompilerResults compilerResult = GenerateCode();

            if (compilerResult.Errors.HasErrors)
                return;

            Type execEnv = compilerResult.CompiledAssembly.GetType(_codeGenerationOptions.Namespace + ".ExecutionEnvironment");
            object myScript = Activator.CreateInstance(execEnv, this);

            myScript.GetType().InvokeMember("Execute", BindingFlags.InvokeMethod, null, myScript, null);
        }

        #endregion

        #region IRuntimeDataProvider Members

        /// <summary>
        /// Get the Data object with the specified name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public T GetData<T>(string identifier) where T: class
        {
            ParameterInfo parameter = new ParameterInfo(identifier, null);
            if (_globalParameterAssignments.ContainsKey(parameter))
                return (T)_globalParameterAssignments[parameter];
            else
                return null;
        }

        /// <summary>
        /// Gets the delegate for the specified identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public Delegate GetMethod(string identifier)
        {
            MethodInfo method = new MethodInfo(identifier, null);

            if (_globalMethodAssignments.ContainsKey(method))
                return _globalMethodAssignments[method];
            else
                return null;
        }

        #endregion
    }
}
