using GameFormatReader.Common;
using OpenTK;
using System.Collections.Generic;
using System.Diagnostics;

namespace GuidanceStone
{
    public class BLWP
    {
        public string FileName { get; set; }

        /* 0x00 */ public string Magic; // PrOD
        /* 0x04 */ public int Unknown0; // Always 0x01000000 <- Version byte with 3 bytes padding?
        /* 0x08 */ public int Unknown1; // Value of 1 (even in empty sblwps)
        /* 0x0C */ public int Unknown2; // ThisVal + 0x10 = start of string data (not offset to string table, but to actual string data)
        /* 0x10 */ public int FileSize;
        /* 0x14 */ public int EntryCount; // Number of InstanceHeader entries in the file.
        /* 0x18 */ public int StringTableOffset;
        /* 0x1C */ public int Padding;

        public StringTable StringTable;
        public IList<InstanceHeader> ObjectInstances;

        public BLWP(string fileName)
        {
            FileName = fileName;
        }

        public void LoadFromStream(EndianBinaryReader reader)
        {
            Magic = reader.ReadChars(4).ToString(); // PrOD
            Unknown0 = reader.ReadInt32();
            Unknown1 = reader.ReadInt32();
            Unknown2 = reader.ReadInt32();
            FileSize = reader.ReadInt32();
            EntryCount = reader.ReadInt32();
            StringTableOffset = reader.ReadInt32();
            Padding = reader.ReadInt32();

            // Validate assumptions.
            Trace.Assert(Unknown0 == 0x01000000);
            Trace.Assert(Unknown1 == 0x00000001);
            Trace.Assert(Padding == 0x00000000);

            ObjectInstances = new List<InstanceHeader>();

            // There are EntryCount many InstanceHeaders (+ data) following
            for (int i = 0; i < EntryCount; i++)
            {
                var size = reader.ReadInt32();
                var instanceCount = reader.ReadInt32();
                var stringOffset = reader.ReadInt32();
                Trace.Assert(reader.ReadInt32() == 0);

                // Read the string name for these instances
                long streamPos = reader.BaseStream.Position;
                reader.BaseStream.Position = StringTableOffset + stringOffset;
                string instanceName = reader.ReadStringUntil('\0');

                InstanceHeader instanceHdr = new InstanceHeader();
                instanceHdr.InstanceName = instanceName;
                ObjectInstances.Add(instanceHdr);

                // Jump back to where we were in our stream and read instanceCount many instances of data.
                reader.BaseStream.Position = streamPos;
                for(int j = 0; j < instanceCount; j++)
                {
                    Instance inst = new Instance();
                    inst.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    inst.Rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    inst.UniformScale = reader.ReadSingle();
                    instanceHdr.Instances.Add(inst);

                    Trace.Assert(reader.ReadInt32() == 0); // Padding
                }
            }
        }
    }

    public class InstanceHeader
    {
        public string InstanceName;
        public IList<Instance> Instances;

        public InstanceHeader()
        {
            Instances = new List<Instance>();
        }
    }

    /// <summary>
    /// Immediately following the <see cref="InstanceHeader"/> is <see cref="InstanceHeader.InstanceCount"/> many instances of that actor.
    /// Each instance has 0x32 bytes of unique data.
    /// </summary>
    public class Instance
    {
        public Vector3 Position;
        public Vector3 Rotation; // In Degrees.
        public float UniformScale;
    }

    /// <summary>
    /// The <see cref="BLWP.StringTableOffset"/> offset is an offset from the start of the file
    /// which points to this class.
    /// </summary>
    public class StringTable
    {
        /* 0x00 */ public int StringCount; // Number of strings in the String Table
        /* 0x04 */ public int StringTableSize; // Size of string table that follows this header (does not include header). Each string is null terminated and then padded up to the next highest 4 byte alignment (even if null termination falls on 4 byte alignment)

        public string[] Strings;
    }
}
