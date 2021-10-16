using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.IO;

namespace Mirle.Agv.INX.Control
{
    public class ShareMemoryWriter
    {
        Dictionary<string, MemoryMappedFile> SMWriter = new Dictionary<string, MemoryMappedFile>();
        object oLock = new object();
        public bool Fun_Ini_ShareMemoryWriter(string MapName)
        {
            if (SMWriter.ContainsKey(MapName) == true) return false;

            //SMWriter.Add(MapName, MemoryMappedFile.CreateNew(@"Global\" + MapName, 1000));
            SMWriter.Add(MapName, MemoryMappedFile.CreateNew(MapName, 1000));
            return true;
        }
        public void Fun_ShareMemoryWriter(string MapName, byte[] Data)
        {
            lock (oLock)
            {
                try
                {
                    using (MemoryMappedViewStream stream = SMWriter[MapName].CreateViewStream())
                    {
                        BinaryWriter writer = new BinaryWriter(stream);
                        writer.Write(Data);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

    }
}
