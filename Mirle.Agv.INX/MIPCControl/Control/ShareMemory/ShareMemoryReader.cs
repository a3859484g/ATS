using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.IO;

namespace Mirle.Agv.INX.Control
{
    public class ShareMemoryReader
    {
        object oLock = new object();
        public byte[] Fun_ShareMemoryReader(string MapName)
        {
            byte[] tmp = new byte[1000];
            lock (oLock)
            {
                try
                {
                    using (MemoryMappedFile SMReader = MemoryMappedFile.OpenExisting(MapName))
                    {

                        using (MemoryMappedViewStream stream = SMReader.CreateViewStream())
                        {
                            BinaryReader reader = new BinaryReader(stream);
                            //tmp = reader.ReadString();
                            tmp = reader.ReadBytes(1000);
                        }
                        return tmp;
                    }
                }
                catch (Exception)
                {
                    return tmp;
                }
            }
        }
    }
}
