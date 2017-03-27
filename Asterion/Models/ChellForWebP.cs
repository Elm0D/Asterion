﻿#define Debug

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;

namespace Asterion.Models
{
    public class ChellForWebP
    {
        //------------- public -----------------------------//
        // Объявляем событие
        public event Action ChangeValueEvent;
        public event Action<int> MaxValueEvent;
        public event Action CompleteConvertEvent;
        public event Action CanceledConvertEvent;

        // Выполняется?
        public bool isRunning = false;
        // Все файлы конвертировать?
        public bool isAllFiles = true;

        public string[] pathFileNames;

        // Качество изображения
        public int quality = 85;

        public int qualityAlpha = 100;

        // Используем метод для запуска события
        private void OnChangeValue()
        {
            ChangeValueEvent();
        }
        private void OnMaxValue( int maxValue )
        {
            MaxValueEvent(maxValue);
        }
        private void OnCompleteConvert()
        {
            CompleteConvertEvent();
        }
        private void OnCanceledConvert()
        {
            CanceledConvertEvent();
        }

        //Process для консольного приложения
        private Process myProcess = null;
        StreamWriter fileLogOut;

        // Команда которую будет выполнять
        string command = string.Empty;
        string commandParameters = string.Empty;

        private string pathToWebp = @"cwebp.exe";

        private string pathDirectory = "";
        private List<string> pathToInputFiles;

        //------------- public -----------------------------//
        public ChellForWebP()
        {
            Initialization();
        }

        /// <summary>
        /// Конвертация выполняется в другом потоке
        /// </summary>
        /// <param name="pathDirectory"></param>
        public void BeginStartConvert( string pathDirectory )
        {
            this.pathDirectory = pathDirectory;
            Thread backgroundThread = new Thread(new ThreadStart(Start));
            backgroundThread.Name = "Вторичный";
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        }

        /// <summary>
        /// Начать конвертацию в том же потоке
        /// </summary>
        /// <param name="pathDirectory"></param>
        public void StartConvert( string pathDirectory )
        {
            this.pathDirectory = pathDirectory;
            Start();
        }

        /// <summary>
        /// Обрабатываются все файлы в папке
        /// </summary>
        public void SwitchOnAllFiles()
        {
            pathFileNames = null;
            isAllFiles = true;
        }

        /// <summary>
        /// Обрабатываются только выбранные файлы в папке
        /// </summary>
        /// <param name="pathFileNames"></param>
        public void SwitchOnSelectedFiles( string[] pathFileNames )
        {
            this.pathFileNames = pathFileNames;
            isAllFiles = false;
        }

        //получение полных путей файлов
        public void ExtractPathsFiles( string pathDirectory )
        {
            FileInfo[] files;
            pathToInputFiles = new List<string>();
            if( isAllFiles )
            {
                DirectoryInfo dir = new System.IO.DirectoryInfo(pathDirectory);
                files = dir.GetFiles();
            }
            else
            {
                int length = this.pathFileNames.Length;
                files = new FileInfo[length];
                for( int i = 0; i < length; i++ )
                {
                    files[i] = new FileInfo(this.pathFileNames[i]);
                }
            }
            foreach( var item in files )
            {
                pathToInputFiles.Add(item.FullName);
            }
        }

        //------------- private -----------------------------//
        private void Initialization()
        {
            myProcess = new Process();
        }

        private void Start()
        {
            ExtractPathsFiles(pathDirectory);
            if( !Directory.Exists(pathDirectory + @"\output") )
            {
                Directory.CreateDirectory(pathDirectory + @"\output");
            }
            // Параметры для Webp конвертера
            commandParameters = string.Format(" -q {0} -alpha_q {1} -o \"{2}{3}",
                    quality,            //{0} -q       качество изображения от 0 до 100
                    qualityAlpha,       //{1} -alpha_q качество изображения для альфа канала от 0 до 100
                    pathDirectory,      //{2}  -o      адрес вывода файла
                    @"\output\"         //{3}          каталог вывода
                );

            List<string> commands = new List<string>();

            OnMaxValue(pathToInputFiles.Count);

            foreach( var currentFile in pathToInputFiles )
            {
                // Компановка команды для Webp конвертера
                command = string.Format(" {1} \"{2}\" {3}{4}.webP\"",
                    "/C",               // {0} Ключ /C - выполнение команды
                    pathToWebp,         // {1} Команда которую будет выполнять
                    currentFile,        // {2} Файл для конвертации
                    commandParameters,  // {3}
                    Path.GetFileNameWithoutExtension(currentFile)  //{4} имя для выходного файла
                    );
                // преобразование кодировки для консоли
                //command = convertToCp866( command );
                commands.Add(command);
            }
            string timeName = DateTime.Now.ToString("HH-mm-ss");
            using( fileLogOut = new StreamWriter(Environment.CurrentDirectory + "\\log-"+ timeName + ".txt") )
            {
                fileLogOut.WriteLine("Начало конвертации в " + DateTime.Now);
                fileLogOut.WriteLine();
                foreach( var command in commands )
                {
                    fileLogOut.WriteLine(new string('=', 50));
                    convertFileToWebP(command);
                    if( !isRunning )
                    {
                        OnCanceledConvert();
                        break;
                    }
                    OnChangeValue();
                }
                fileLogOut.WriteLine("Конец конвертации в " + DateTime.Now);
            }
            if( isRunning )
                OnCompleteConvert();
            // Очистка старых событий;
            ClearEvents();
        }
        private string convertToCp866( string input )
        {
            Encoding cp866 = Encoding.GetEncoding(866);
            Encoding unicode = Encoding.Unicode;

            // Convert the string into a byte array.
            byte[] unicodeBytes = unicode.GetBytes(input);

            // Perform the conversion from one encoding to the other.
            byte[] cp866Bytes = Encoding.Convert(unicode, cp866, unicodeBytes);

            // Convert the new byte[] into a char[] and then into a string.
            char[] cp866Chars = new char[cp866.GetCharCount(cp866Bytes, 0, cp866Bytes.Length)];
            cp866.GetChars(cp866Bytes, 0, cp866Bytes.Length, cp866Chars, 0);
            string cp866String = new string(cp866Chars);

            return cp866String;
        }

        private void convertFileToWebP( string command )
        {
            // Запускаем через cmd с параметрами command
            ProcessStartInfo startinfo = new ProcessStartInfo();
            startinfo.StandardOutputEncoding = Encoding.GetEncoding(866);

            startinfo.FileName = @"C:\Windows\System32\cmd.exe";
            // скрываем окно запущенного процесса
            startinfo.WindowStyle = ProcessWindowStyle.Hidden;
            // Перенаправить вывод
            startinfo.RedirectStandardOutput = true;
            startinfo.RedirectStandardInput = true;
            startinfo.RedirectStandardError = true;
            // Не используем shellexecute
            startinfo.UseShellExecute = false;
            // Не надо окон
            startinfo.CreateNoWindow = true;
            
            myProcess.StartInfo = startinfo;
            myProcess.OutputDataReceived += cmd_DataReceived;
            myProcess.ErrorDataReceived += cmd_DataError;
            myProcess.EnableRaisingEvents = true;

            // запускаем процесс
            myProcess.Start();
            myProcess.BeginOutputReadLine();
            myProcess.BeginErrorReadLine();

            myProcess.StandardInput.WriteLine("cwebp.exe " + command);            
            myProcess.StandardInput.WriteLine("exit");
            myProcess.WaitForExit();

            // Освобождаем
            myProcess = null;
            startinfo = null;
        }

        private void ClearEvents()
        {
            ChangeValueEvent = null;
            MaxValueEvent = null;
            CompleteConvertEvent = null;
            CanceledConvertEvent = null;
        }

        void cmd_DataReceived( object sender, DataReceivedEventArgs e )
        {
            try
            {
                using( StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + "\\log1.txt", true, Encoding.UTF8) )
                {
                    //Выводим                
                    sw.WriteLine(e.Data);
                    sw.Close();
                }
            } catch {  }
        }

        void cmd_DataError( object sender, DataReceivedEventArgs e )
        {
            try
            {
                //Пишем в файл(поток)                
                fileLogOut.WriteLine(e.Data);
            } catch {}
        }
    }
}

