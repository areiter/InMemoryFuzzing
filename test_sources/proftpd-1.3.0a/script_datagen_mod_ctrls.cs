#import System.IO
#endheader

using(MemoryStream sink = new MemoryStream())
{
  StreamHelper.WriteInt32(0, sink);    //status
  StreamHelper.WriteUInt32(1, sink);   //number of arguments

  if(IsValueSet("scriptval_generate_valid_data") && 
     (string)GetValue("scriptval_generate_valid_data") == "1")
  {
    StreamHelper.WriteUInt32(100, sink); //data length

    //Write 100 bytes of data, no overflow
    byte[] data = new byte[100];
    sink.Write(data, 0, data.Length);
  }
  else
  {
    uint lastLength = 100;
    if( IsValueSet("last_length") )
      lastLength = (uint)GetValue("last_length");

    //increase the length of data
    lastLength += 50;
    SetValue("last_length", lastLength);

    byte[] data = new byte[lastLength]; //data 
    Random r = new Random();
    r.NextBytes(data);

    StreamHelper.WriteUInt32(lastLength, sink);
    sink.Write(data, 0, data.Length);
  }    

  
  SetData(sink.ToArray());
}



