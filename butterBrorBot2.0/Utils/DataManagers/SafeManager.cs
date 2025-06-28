using DankDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.DataManagers
{
    public static class SafeManager
    {
        public static void Save(string filePath, string key, object data, bool createBackup = true)
        {
            if (FileUtil.FileExists(filePath) && createBackup)
            {
                FileUtil.CreateBackup(filePath);
            }
            Manager.Save(filePath, key, data);
        }
    }
}
