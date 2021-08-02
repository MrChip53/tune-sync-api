using System;
using System.Collections.Generic;
using System.IO;

namespace TuneSyncAPI
{

	public class Util
	{
		public Util()
		{
		}

		public static List<String> scanDirectory(String folder)
        {
            List<String> folderFileList = new List<String>(Directory.GetFiles(folder));

            string[] folderList = Directory.GetDirectories(folder);

            foreach (String directory in folderList)
            {
                //string[] files = Directory.GetFiles(directory);

                List<String> files = scanDirectory(directory);

                foreach (String file in files)
                {
                    folderFileList.Add(file);
                }
                

            }

            return folderFileList;
        }
	}

}
