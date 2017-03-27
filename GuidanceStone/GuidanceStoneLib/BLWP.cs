using GameFormatReader.Common;
using OpenTK;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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

        /// <summary>
        /// Saves the BLWP file to memory.
        /// </summary>
        /// <returns>BLWP data</returns>
        public byte[] SaveToMemory()
        {
            byte[] file;

            using (MemoryStream stream = new MemoryStream())
            {
                EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

                writer.Write(System.Text.Encoding.UTF8.GetBytes("PrOD")); // Magic
                writer.Write(0x01000000); // Version number and padding?
                writer.Write(1); // Unknown, always 1

                writer.Write(0); // Placeholder for offset to string table data
                writer.Write(0); // Placeholder for file size
                writer.Write(ObjectInstances.Count); // Number of instances
                writer.Write(0); // Placeholder for string table offset
                writer.Write(0); // Padding, always 0

                // This MemoryStream will hold our string table data as we add it
                using (System.IO.MemoryStream stringTable = new System.IO.MemoryStream())
                {
                    EndianBinaryWriter stringWriter = new EndianBinaryWriter(stringTable, Endian.Big);

                    // Write instance data
                    foreach (InstanceHeader head in ObjectInstances)
                    {
                        writer.Write(head.Instances.Count * 32); // Size of instances in bytes
                        writer.Write(head.Instances.Count); // Number of instances
                        writer.Write((int)stringWriter.BaseStream.Position + 8); // Offset to name of the instance in the string table.

                        stringWriter.WriteFixedString(head.InstanceName, head.InstanceName.Length); // Add name to string table
                        stringWriter.Write((byte)0); // Null terminator for string

                        // Strings are padded to the nearest 4. We apply the padding algorithm:
                        // (x + (n - 1)) & ~(n - 1)
                        // Then we subtract the original value (x) to get the size of the padding.
                        // Here, x is (head.InstanceName.Length + 1) to account for the null, and n is 4. We simplify 4 - 1 to 3.
                        int alignment = ((((head.InstanceName.Length + 1) + 3) & ~(3)) - (head.InstanceName.Length + 1));
                        byte[] stringPadding = new byte[alignment];
                        stringWriter.Write(stringPadding);

                        writer.Write(0); // Padding

                        foreach (Instance inst in head.Instances)
                        {
                            // Position
                            writer.Write(inst.Position.X);
                            writer.Write(inst.Position.Y);
                            writer.Write(inst.Position.Z);

                            // Rotation, in degrees
                            writer.Write(inst.Rotation.X);
                            writer.Write(inst.Rotation.Y);
                            writer.Write(inst.Rotation.Z);

                            // Uniform scale
                            writer.Write(inst.UniformScale);

                            // Padding, always 0
                            writer.Write(0);
                        }
                    }

                    // Write string table data offset
                    writer.BaseStream.Position = 0x0C;
                    writer.Write((int)writer.BaseStream.Length - 8);

                    // Write string table offset
                    writer.BaseStream.Position = 0x18;
                    writer.Write((int)writer.BaseStream.Length);

                    writer.BaseStream.Seek(0, System.IO.SeekOrigin.End); // Return to end of stream to write string table

                    // Write string table data
                    writer.Write(ObjectInstances.Count);
                    writer.Write((int)stringWriter.BaseStream.Length);
                    writer.Write(stringTable.ToArray());

                    // Write file size
                    writer.BaseStream.Position = 0x10;
                    writer.Write((int)writer.BaseStream.Length);

                    writer.BaseStream.Seek(0, System.IO.SeekOrigin.End); // Finish by returning to the end of the stream

                    file = stream.ToArray();
                }
            }

            return file;
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
