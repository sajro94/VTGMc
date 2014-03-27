using System;
using System.IO;
using Microsoft.Win32;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            username.Text = Properties.Settings.Default.username;
            password.Password = Properties.Settings.Default.password;
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            int count = dir.GetFiles().Length;
            if(count > 1)
            {
                string specificFolder = AppDomain.CurrentDomain.BaseDirectory;
                if (Properties.Settings.Default.installed != "installed")
                {
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    string advarsel = "Launcheren bør være i en mappe for sig selv, ønsker du at annulere installationen af modpack og flytte launcheren?";
                    DialogResult result = System.Windows.Forms.MessageBox.Show(advarsel, "Advarsel", buttons);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        this.Close();
                    }
                    else
                    {
                        Properties.Settings.Default.installed = "installed";
                    }
                }
            }
            ulong max_ram = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            var max_ram_mb = max_ram / 1024 / 1024;
            //MessageBox.Show(max_ram_mb.ToString("n"));
            var ram_available = max_ram_mb - 1024;
            ram_slider.Maximum = ram_available;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(stay_logged_in.IsChecked == true)
            {
                Properties.Settings.Default.username = username.Text;
                Properties.Settings.Default.password = password.Password;
            }
            else if(stay_logged_in.IsChecked == false)
            {
                Properties.Settings.Default.username = null;
                Properties.Settings.Default.password = null;
            }
            Properties.Settings.Default.Save();
            process();
        }
        private async Task process()
        {
                string sessionId;
                string user = username.Text;
                string pass = password.Password;
                Console.WriteLine(pass);
                System.Net.WebClient wc = new System.Net.WebClient();
                string webData = wc.DownloadString("http://login.minecraft.net/?user="+user+"&password="+pass+"&version=13");
                Properties.Settings.Default.session_id = webData;
                Console.WriteLine(webData);
                string[] sessions = webData.Split(':');
                if (sessions.Length > 1)
                {
                    sessionId = sessions[3];
                }
                else
                {
                    sessionId = sessions[0];
                }
                var valid = wc.DownloadString("http://session.minecraft.net/game/joinserver.jsp?user="+user+"&sessionId="+sessionId+"&serverId=1");
                foreach (string s in sessions)
                {
                    Console.WriteLine(s);
                }
                if (valid == "OK")
                {
                    Console.WriteLine("Succesful Login");
                    string specificFolder = AppDomain.CurrentDomain.BaseDirectory;
                    string zip_path = System.IO.Path.Combine(specificFolder, "VTGLAN.zip");
                    if (!File.Exists(zip_path))
                    {
                        await clean_folder();
                        await download_files();
                    }
                    else
                    {
                     HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://sajro.org/VTGLAN/VTGLAN/VTGLAN.zip");
                     req.Method = "HEAD";
                     HttpWebResponse resp = (HttpWebResponse)(req.GetResponse());
                     long web_len = resp.ContentLength;
                     FileInfo zip_path_size = new FileInfo(zip_path);
                     long local_len = zip_path_size.Length;
                        if(local_len == web_len)
                        {
                            if (File.Exists(System.IO.Path.Combine(specificFolder, "version.html")))
                            {
                                string version_web = wc.DownloadString("http://sajro.org/VTGLAN/VTGLAN/version.html");
                                string version_local = wc.DownloadString(System.IO.Path.Combine(specificFolder, "version.html"));
                                if (version_local == version_web)
                                {
                                    Completed(null, null);
                                }
                                else
                                {
                                    File.Delete(zip_path);
                                    await clean_folder();
                                    await download_files();

                                }
                            }
                            else
                            {
                                File.Delete(zip_path);
                                await clean_folder();
                                await download_files();
                            }

                        }
                        else
                        {
                            File.Delete(zip_path);
                            await clean_folder();
                            await download_files();
                        }
                    }

                }
                else
                {
                    System.Windows.MessageBox.Show("Bad Login!!!!");
                }
        }
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            download_progress.Value = e.ProgressPercentage;
            
        }
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            string specificFolder = AppDomain.CurrentDomain.BaseDirectory;
            string zip_path = System.IO.Path.Combine(specificFolder, "VTGLAN.zip");
            string jar_path = System.IO.Path.Combine(specificFolder, "bin/modpack.jar");
            if(!File.Exists(jar_path))
            {
                extract();
            }
            string javaPath;
            string DIR = specificFolder.TrimEnd('\\');
            string user = username.Text;
            string pass = password.Password;
            string webData = Properties.Settings.Default.session_id;
            string ram_amount = ram_slider.Value.ToString("g");
            int user_length = user.Length + 1;
            var user_index = webData.LastIndexOf(user) + user_length;
            string token = webData.Substring(user_index);
            string javaType = "javaw";
            if(console.IsChecked == true)
            {
                javaType = "java";
            }
            Process launcher = new Process();
            if (File.Exists(@"C:\Program Files\java\jre7\bin\" + javaType + ".exe"))
            {
                javaPath = @"C:\Program Files\java\jre7\bin\" + javaType + ".exe";
            }
            else if (File.Exists(@"C:\Program Files (x86)\java\jre7\bin\" + javaType + ".exe"))
            {
                javaPath = @"C:\Program Files (x86)\java\jre7\bin\" + javaType + ".exe";
                ram_amount = "512";
                System.Windows.MessageBox.Show("Ram has been set to 512M because you are using a 32-bit version of java\nPlease try and install the 64-bit version");
            }
            else
            {
                string installPath = GetJavaInstallationPath();
                javaPath = System.IO.Path.Combine(installPath, @"bin\" + javaType + ".exe");
            }
            if (!javaPath.Contains("jre7"))
            {
                System.Windows.MessageBox.Show("You might have to upgrade your java to java7\nI will try to start Minecraft\nBut cant promise anything");
            }
                System.Windows.MessageBox.Show(javaPath);
                launcher.StartInfo.FileName = javaPath;
                launcher.StartInfo.Arguments = "-Xmx" + ram_amount + "m -XX:MaxPermSize=256m -Djava.library.path=" + DIR + "\\bin\\natives -Dminecraft.applet.TargetDirectory=" + DIR + " -cp " + DIR + "\\natives\\net\\minecraft\\launchwrapper\\1.8\\launchwrapper-1.8.jar;" + DIR + "\\natives\\org\\ow2\\asm\\asm-all\\4.1\\asm-all-4.1.jar;" + DIR + "\\natives\\org\\scala-lang\\scala-library\\2.10.2\\scala-library-2.10.2.jar;" + DIR + "\\natives\\org\\scala-lang\\scala-compiler\\2.10.2\\scala-compiler-2.10.2.jar;" + DIR + "\\natives\\lzma\\lzma\\0.0.1\\lzma-0.0.1.jar;" + DIR + "\\natives\\net\\sf\\jopt-simple\\jopt-simple\\4.5\\jopt-simple-4.5.jar;" + DIR + "\\natives\\com\\paulscode\\codecjorbis\\20101023\\codecjorbis-20101023.jar;" + DIR + "\\natives\\com\\paulscode\\codecwav\\20101023\\codecwav-20101023.jar;" + DIR + "\\natives\\com\\paulscode\\libraryjavasound\\20101123\\libraryjavasound-20101123.jar;" + DIR + "\\natives\\com\\paulscode\\librarylwjglopenal\\20100824\\librarylwjglopenal-20100824.jar;" + DIR + "\\natives\\com\\paulscode\\soundsystem\\20120107\\soundsystem-20120107.jar;" + DIR + "\\natives\\argo\\argo\\2.25_fixed\\argo-2.25_fixed.jar;" + DIR + "\\natives\\org\\bouncycastle\\bcprov-jdk15on\\1.47\\bcprov-jdk15on-1.47.jar;" + DIR + "\\natives\\com\\google\\guava\\guava\\14.0\\guava-14.0.jar;" + DIR + "\\natives\\org\\apache\\commons\\commons-lang3\\3.1\\commons-lang3-3.1.jar;" + DIR + "\\natives\\commons-io\\commons-io\\2.4\\commons-io-2.4.jar;" + DIR + "\\natives\\net\\java\\jinput\\jinput\\2.0.5\\jinput-2.0.5.jar;" + DIR + "\\natives\\net\\java\\jutils\\jutils\\1.0.0\\jutils-1.0.0.jar;" + DIR + "\\natives\\com\\google\\code\\gson\\gson\\2.2.2\\gson-2.2.2.jar;" + DIR + "\\natives\\org\\lwjgl\\lwjgl\\lwjgl\\2.9.0\\lwjgl-2.9.0.jar;" + DIR + "\\natives\\org\\lwjgl\\lwjgl\\lwjgl_util\\2.9.0\\lwjgl_util-2.9.0.jar;" + DIR + "\\bin\\modpack.jar;" + DIR + "\\bin\\minecraft.jar net.minecraft.launchwrapper.Launch --username " + user + " --session token:" + token + " --version 1.6.4-Forge9.11.1.965 --gameDir " + DIR + " --assetsDir " + DIR + "\\assets --tweakClass cpw.mods.fml.common.launcher.FMLTweaker";
                //May need to be placed in Arguments:  -Dfml.core.libraries.mirror=http://mirror.technicpack.net/Technic/lib/fml/%s
                launcher.Start();

        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }
        private void extract()
        {

            string specificFolder = AppDomain.CurrentDomain.BaseDirectory;
            string tempfolder = System.IO.Path.Combine(specificFolder, "temp");
            string zip_path = System.IO.Path.Combine(specificFolder, "VTGLAN.zip");
            Directory.CreateDirectory(tempfolder);
            ZipFile.ExtractToDirectory(zip_path,tempfolder);
            foreach (string dirPath in Directory.GetDirectories(tempfolder, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(tempfolder, specificFolder));
            }
            foreach (string newPath in Directory.GetFiles(tempfolder, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(tempfolder, specificFolder), true);
            }
            Directory.Delete(tempfolder,true);
        }
        private async Task download_files()
        {
            download_progress.Opacity = 1.0;
            System.Net.WebClient wc = new System.Net.WebClient();
            string specificFolder = AppDomain.CurrentDomain.BaseDirectory;
            string zip_path = System.IO.Path.Combine(specificFolder, "VTGLAN.zip");
            wc.DownloadFileAsync(new Uri("http://sajro.org/VTGLAN/VTGLAN/VTGLAN.zip"), zip_path);
            wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
            wc.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
        }
        private async Task clean_folder()
        {
            string specificFolder = AppDomain.CurrentDomain.BaseDirectory;
            if(File.Exists(System.IO.Path.Combine(specificFolder, "server.jar")))
            {
            File.Delete(System.IO.Path.Combine(specificFolder, "server.jar"));
            }
            if(File.Exists(System.IO.Path.Combine(specificFolder, "server_run.bat")))
            {
            File.Delete(System.IO.Path.Combine(specificFolder, "server_run.bat"));
            }
            if(File.Exists(System.IO.Path.Combine(specificFolder, "version.html")))
            {
            File.Delete(System.IO.Path.Combine(specificFolder, "version.html"));
            }
            if(Directory.Exists(System.IO.Path.Combine(specificFolder, "mods")))
            {
            Directory.Delete(System.IO.Path.Combine(specificFolder, "mods"),true);
            }
            if(Directory.Exists(System.IO.Path.Combine(specificFolder, "assets")))
            {
            Directory.Delete(System.IO.Path.Combine(specificFolder, "assets"), true);
            }
            if(Directory.Exists(System.IO.Path.Combine(specificFolder, "bin")))
            {
            Directory.Delete(System.IO.Path.Combine(specificFolder, "bin"), true);
            }
            if(Directory.Exists(System.IO.Path.Combine(specificFolder, "config")))
            {
            Directory.Delete(System.IO.Path.Combine(specificFolder, "config"), true);
            }
            if(Directory.Exists(System.IO.Path.Combine(specificFolder, "libraries")))
            {
            Directory.Delete(System.IO.Path.Combine(specificFolder, "libraries"), true);
            }
            if (Directory.Exists(System.IO.Path.Combine(specificFolder, "natives")))
            {
            Directory.Delete(System.IO.Path.Combine(specificFolder, "natives"), true);
            }
        }
        private String GetJavaInstallationPath()
        {
            String javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(javaKey))
            {
                using (var homeKey = baseKey.OpenSubKey("1.7"))
                    return homeKey.GetValue("JavaHome").ToString();
            }
        }
    }
}
    