using GuidanceStone;
using System;
using System.IO;
using WArchiveTools;

namespace GuidanceStoneTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //string filePath = @"E:\BreathOfTheWild\WiiUDiskImage\content\Map\MainField\A-1\A-1.11_Clustering.sblwp";
            string rootDirectory = @"E:\BreathOfTheWild\WiiUDiskImage\content\Map\MainField";
            foreach(var folder in Directory.GetDirectories(rootDirectory))
            {
                foreach(var file in Directory.GetFiles(folder, "*.sblwp"))
                {
                    Console.WriteLine($"Opening file {Path.GetFileName(file)}...");
                    using (var fileStream = FileUtilities.LoadFile(file))
                    {
                        BLWP blwpFile = new BLWP(Path.GetFileNameWithoutExtension(file));
                        blwpFile.LoadFromStream(fileStream);

                        foreach(var instanceHeader in blwpFile.ObjectInstances)
                        {
                            //Console.WriteLine($"Instance Name: {instanceHeader.InstanceName} Count: {instanceHeader.Instances.Count}");

                            int count = 0;
                            foreach (var instance in instanceHeader.Instances)
                            {
                                //Console.WriteLine($"{count}");
                                //Console.WriteLine($"\tPosition: {instance.Position}");
                                //Console.WriteLine($"\tRotation: {instance.Rotation}");
                                //Console.WriteLine($"\tUniScale: {instance.UniformScale}");
                                count++;
                            }
                        }
                    }
                }
            }
        }
    }
}
