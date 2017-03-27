using GuidanceStone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WArchiveTools;

namespace GuidanceStoneTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = @"E:\BreathOfTheWild\WiiUDiskImage\content\Map\MainField\A-1\A-1.11_Clustering.sblwp";

            using (var fileStream = FileUtilities.LoadFile(filePath))
            {
                BLWP blwpFile = new BLWP();
                blwpFile.LoadFromStream(fileStream);

                foreach(var instanceHeader in blwpFile.ObjectInstances)
                {
                    Console.WriteLine($"Instance Name: {instanceHeader.InstanceName} Count: {instanceHeader.Instances.Count}");

                    int count = 0;
                    foreach (var instance in instanceHeader.Instances)
                    {
                        Console.WriteLine($"{count}");
                        Console.WriteLine($"\tPosition: {instance.Position}");
                        Console.WriteLine($"\tRotation: {instance.Rotation}");
                        Console.WriteLine($"\tUniScale: {instance.UniformScale}");
                        count++;
                    }
                }
            }
        }
    }
}
