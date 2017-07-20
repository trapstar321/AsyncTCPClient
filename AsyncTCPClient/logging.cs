using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace AsyncTCPClient
{
    public enum log_vrste
    {
        greska = 0,
        info = 1,
        warning = 2
    }

    public class LogRecord
    {
        public log_vrste vrsta;
        public string opis;
        public string prefix;
        public DateTime date;

        public LogRecord(log_vrste vrsta, string opis, string prefix)
        {
            this.vrsta = vrsta;
            this.opis = opis;
            this.prefix = prefix;
            date = DateTime.Now;
        }
    }

    public class logging
    {
        private List<LogRecord> items = new List<LogRecord>();
        private Timer timer = new Timer();

        private string _prefix = "";
        private FileStream fs;
        private const int max_generations = 10;
        private string file_base = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);

        private bool currently_versioning = false;
        public delegate bool add_to_log_del(List<LogRecord> items);
        private bool add_to_log_fnc(List<LogRecord> items)
        {
            if (fs.CanWrite)
            {
                StreamWriter s = new StreamWriter(fs);
                foreach (LogRecord record in items)
                {
                    System.DateTime datum = DateTime.Now;

                    switch (record.vrsta)
                    {
                        case log_vrste.greska:
                            s.WriteLine(record.date.ToString("dd.MM.yyyy HH:mm:ss.fff") + "\t" + "ERROR" + "\t" + _prefix + " " + record.prefix + " " + record.opis);
                            break;
                        case log_vrste.info:
                            s.WriteLine(record.date.ToString("dd.MM.yyyy HH:mm:ss.fff") + "\t" + "INFO" + "\t" + _prefix + " " + record.prefix + " " + record.opis);
                            break;
                        case log_vrste.warning:
                            s.WriteLine(record.date.ToString("dd.MM.yyyy HH:mm:ss.fff") + "\t" + "WARNING" + "\t" + _prefix + " " + record.prefix + " " + record.opis);
                            break;
                            //Case FiskalS_interfaces.interf_classlogging.log_vrste.greska
                            //    s.WriteLine(FormatDateTime(datum, DateFormat.GeneralDate) & vbTab & "ERROR" & vbTab & _prefix & " " & prefix & " " & opis)
                            //Case FiskalS_interfaces.interf_classlogging.log_vrste.info
                            //    s.WriteLine(FormatDateTime(datum, DateFormat.GeneralDate) & vbTab & "INFO" & vbTab & _prefix & " " & prefix & " " & opis)
                            //Case FiskalS_interfaces.interf_classlogging.log_vrste.warning
                            //    s.WriteLine(FormatDateTime(datum, DateFormat.GeneralDate) & vbTab & "WARNING" & vbTab & _prefix & " " & prefix & " " & opis)
                    }
                }
                s.Flush();
                fs.Flush();
                bool filebig = false;
                if (fs.Length > 5000000)
                {
                    filebig = true;
                }
                if (filebig == true & currently_versioning == false)
                {
                    currently_versioning = true;
                    generacijaplus();
                    File.Copy(file_base + "\\log.txt", file_base + "\\log1.txt", true);
                    fs.Close();
                    File.Delete(file_base + "\\log.txt");
                    fs = new FileStream(file_base + "\\log.txt", FileMode.Append, FileAccess.Write, FileShare.Read);
                    currently_versioning = false;
                }
            }
            return true;
        }
        private void generacijaplus()
        {
            //top most number goes to +1
            int a = 0;
            if (find_last_log() >= max_generations)
            {
                File.Delete(file_base + "\\log" + find_last_log() + ".txt");
            }
            for (a = find_last_log(); a >= 1; a += -1)
            {
                if (File.Exists(file_base + "\\log" + a + 1 + ".txt") == true)
                    File.Delete(file_base + "\\log" + a + 1 + ".txt");
                File.Move(file_base + "\\log" + a + ".txt", file_base + "\\log" + a + 1 + ".txt");
            }
        }

        private int find_last_log()
        {
            int a = 0;
            for (a = 1; a <= max_generations + 2; a++)
            {
                if (File.Exists(file_base + "\\log" + a + ".txt") == false)
                    return a - 1;
            }
            return 0;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<LogRecord> tmp = new List<LogRecord>();
            lock (items)
            {
                tmp.AddRange(items);
                items.Clear();
            }

            add_to_log_del zapis = new add_to_log_del(add_to_log_fnc);
            zapis.BeginInvoke(tmp, null, null);
        }

        public logging(string prefix)
        {
            _prefix = prefix;
            fs = new FileStream(file_base + "\\log.txt", FileMode.Append, FileAccess.Write, FileShare.Read);

            timer.Interval += 100;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public logging(string appName, string cash_register)
        {
            string filename = appName + "_" + cash_register + "_" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
            fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
            timer.Interval += 100;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public void add_to_log(log_vrste vrsta, string opis, string prefix)
        {
            lock (items)
            {
                items.Add(new LogRecord(vrsta, opis, prefix));
            }
            /*add_to_log_del zapis = new add_to_log_del(add_to_log_fnc);
            zapis.BeginInvoke(vrsta, opis, prefix, null, null);*/
        }
    }
}