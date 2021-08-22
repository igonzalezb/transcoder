using System;
using Microsoft.Win32;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using static System.Environment;
using System.Security.Permissions;

namespace Transcoder
{
    public class ContextMenu
    {
        private const string FolderMenuName = "Directory\\shell\\Transcode";
        private const string FileMenuName = "*\\shell\\Transcode";

        public static void setContextMenu(string onOff)
        {
            if(onOff.Equals("on"))
            {
                setContextMenuOff(FileMenuName);
                setContextMenuOff(FolderMenuName);
                setContextMenuOn(FileMenuName);
                setContextMenuOn(FolderMenuName);
            }
                
            else if (onOff.Equals("off"))
            {
                setContextMenuOff(FileMenuName);
                setContextMenuOff(FolderMenuName);
            }
                
            else
                Console.WriteLine("Error, input must be <on> or <off>");
        }

        private static void setContextMenuOn(string MainMenuName)
        {
            string Command = "cmd /c transcoder \"%1\"";
            
            RegistryKey regmenu = null;
            //RegistryKey regcmd = null;
            
            try
            {
                regmenu = Registry.ClassesRoot.CreateSubKey(MainMenuName);
                if(regmenu != null)
                {
                    string path = System.AppContext.BaseDirectory;
                    regmenu.SetValue("Icon", $"\"{path}icon.ico\",0");
                    regmenu = regmenu.CreateSubKey("command");
                    regmenu.SetValue("", Command);
                }
                //regmenu = regmenu.CreateSubKey("shell");
                //regcmd = regmenu.CreateSubKey("Rename Movies\\command");
                //if(regcmd != null)
                //    regcmd.SetValue("", Command);
                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally       
            {
                if(regmenu != null)
                    regmenu.Close();
                // if(regcmd != null)
                //     regcmd.Close();
            }
        }

        private static void setContextMenuOff(string MainMenuName)
        {   
            try
            {
                RegistryKey reg = Registry.ClassesRoot.OpenSubKey(MainMenuName);
                if(reg != null)
                {
                    reg.Close();
                    Registry.ClassesRoot.DeleteSubKeyTree(MainMenuName);
                }
            }
            catch(Exception ex)
            {
               Console.WriteLine(ex.Message);
            }
            
        }
    }
}
