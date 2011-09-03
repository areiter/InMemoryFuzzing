using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using DevEck.ScriptingEngine.Environment;

namespace DevEck.ScriptingEngine.Common
{
    /// <summary>
    /// Some Helpers for generating common Code statements
    /// </summary>
    public static class CodeHelpers
    {
        public enum ModifierEnum
        {
            Private = MemberAttributes.Private, 
            Public = MemberAttributes.Public,
            Internal = MemberAttributes.Assembly,
            Protected = MemberAttributes.Family
        }

        /// <summary>
        /// Defines a member variable
        /// </summary>
        /// <param name="container"></param>
        /// <param name="modifier"></param>
        /// <param name="typeInfo"></param>
        /// <returns></returns>
        public static CodeMemberField DefineField(CodeTypeDeclaration container,
            ModifierEnum modifier, ParameterInfo typeInfo)
        {
            CodeMemberField member = new CodeMemberField(new CodeTypeReference(typeInfo.ParameterType), typeInfo.Name);
            member.Attributes = (member.Attributes & ~MemberAttributes.AccessMask) | (MemberAttributes)modifier;
            container.Members.Add(member);
            return member;
        }

        /// <summary>
        /// Defines a property
        /// </summary>
        /// <param name="container"></param>
        /// <param name="modifier"></param>
        /// <param name="typeInfo"></param>
        /// <param name="hasGet"></param>
        /// <param name="hasSet"></param>
        /// <returns></returns>
        public static CodeMemberProperty DefineProperty(CodeTypeDeclaration container,
            ModifierEnum modifier, ParameterInfo typeInfo, bool hasGet, bool hasSet)
        {
            CodeMemberProperty member = new CodeMemberProperty();
            member.HasGet = hasGet;
            member.HasSet = hasSet;
            member.Name = typeInfo.Name;
            member.Type = new CodeTypeReference(typeInfo.ParameterType);
            member.Attributes = (member.Attributes & ~MemberAttributes.AccessMask) | (MemberAttributes)modifier;

            container.Members.Add(member);
            return member;
        }

        /// <summary>
        /// Defines an accessor (get/set)
        /// </summary>
        /// <param name="container"></param>
        /// <param name="modifier"></param>
        /// <param name="typeInfo"></param>
        /// <param name="hasGet"></param>
        /// <param name="hasSet"></param>
        /// <param name="memberVariableName"></param>
        /// <returns></returns>
        public static CodeMemberProperty DefineAccessor(CodeTypeDeclaration container,
            ModifierEnum modifier, ParameterInfo typeInfo, bool hasGet, bool hasSet, string memberVariableName)
        {
            CodeMemberProperty property = DefineProperty(container, modifier, typeInfo, hasGet, hasSet);

            if (hasGet)
            {
                property.GetStatements.Add(new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(), memberVariableName
                    )
                )
                );
            }

            if (hasSet)
            {
                property.SetStatements.Add(new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), memberVariableName),
                    new CodePropertySetValueReferenceExpression()));
            }

            return property;
        }

        /// <summary>
        /// Defines and returns a member method with the given options
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="returnType">Return type of the method</param>
        /// <param name="parameters">Parameters of the method</param>
        /// <returns></returns>
        public static CodeMemberMethod DefineMemberMethod(CodeTypeDeclaration container, 
            string name, ModifierEnum modifier, ParameterInfo returnType, params ParameterInfo[] parameters)
        {
            CodeMemberMethod member = new CodeMemberMethod();
            member.Name = name;
            member.Attributes = (member.Attributes & ~MemberAttributes.AccessMask) | (MemberAttributes)modifier;

            if (returnType == null || returnType.ParameterType == null)
                member.ReturnType = new CodeTypeReference(typeof(void));
            else
                member.ReturnType = new CodeTypeReference(returnType.ParameterType);

            if (parameters != null)
            {
                foreach (ParameterInfo parameter in parameters)
                {
                    CodeParameterDeclarationExpression codeParameter = new CodeParameterDeclarationExpression(
                        parameter.ParameterType, parameter.Name);
                    member.Parameters.Add(codeParameter);
                }
            }

            container.Members.Add(member);
            return member;
        }

        /// <summary>
        /// Defines and returns a constructor for the given type
        /// </summary>
        /// <param name="container"></param>
        /// <param name="modifier"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static CodeConstructor DefineConstructor(CodeTypeDeclaration container,
            ModifierEnum modifier, params ParameterInfo[] parameters)
        {
            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = (ctor.Attributes & ~MemberAttributes.AccessMask) | (MemberAttributes)modifier;

            if (parameters != null)
            {
                foreach (ParameterInfo parameter in parameters)
                {
                    CodeParameterDeclarationExpression codeParameter = new CodeParameterDeclarationExpression(
                        parameter.ParameterType, parameter.Name);
                    ctor.Parameters.Add(codeParameter);
                }
            }

            container.Members.Add(ctor);
            return ctor;
        }
    }
}
