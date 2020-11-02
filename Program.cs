using System;
using System.IO;
using System.Security.Permissions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Reflection;

public class Param_t
{
    public string src_directory { get; set; }
    public string output_directory { get; set; }
    public bool initialization { get; set; }
    public List<string> init_dir_list { get; set; }
    public string pandoc_exec { get; set; }
    public string pandoc_html_template { get; set; }
    public string index_html_template { get; set; }
    public List<string> tree_hide_dir_list { get; set; }
    public List<string> tree_show_file_ext_list { get; set; }
    public List<string> search_hide_dir_list { get; set; }
    public List<string> avoid_copy_ext_list { get; set; }
}
public class Watcher
{
    //
    public static Param_t param_;
    //
    public static string cwd_;
    public static string src_fdir_;
    public static string output_fdir_;
    //
    // public static Assembly currentAssem;
    //
    public static List<string> categories_;
    //
    public static List<string> markdown_ext_list;
    //
    private const int n_retry_file = 3;
    private const int delay_retry = 1000;
    //
    public static void Main()
    {
        //
        Init();
        //
        Run();
    }

    /*
     * Set up
     */
    private static void Init()
    {
        string[] args = Environment.GetCommandLineArgs();
        // If a directory is not specified, exit program.
        if (args.Length != 2)
        {
            // Display the proper way to call the program.
            Console.WriteLine("Usage: NotesMonitor.exe (config.json)");
            Environment.Exit(-1);
        }
        // currentAssem = Assembly.GetExecutingAssembly();
        // string json_fpath = Path.Join(currentAssem.Location, "monitor.json");
        string json_fpath = Path.GetFullPath(args[1]);
        StreamReader json_file = File.OpenText(json_fpath);
        param_ = JsonConvert.DeserializeObject<Param_t>(json_file.ReadToEnd());
        //
        cwd_ = Path.GetDirectoryName(json_fpath);
        src_fdir_ = Path.GetFullPath(Path.Join(cwd_, param_.src_directory));
        output_fdir_ = Path.GetFullPath(Path.Join(cwd_, param_.output_directory));
        // Set up markdown_ext_list
        markdown_ext_list = new List<string>();
        markdown_ext_list.Add(".md");
        markdown_ext_list.Add(".markdown");
        markdown_ext_list.Add(".mkd");
        // We should use src_fdir_ to obtain the categories
        var folders = Directory.EnumerateDirectories(src_fdir_);
        categories_ = new List<string>();
        foreach (var f in folders)
        {
            foreach (var ign in param_.search_hide_dir_list)
            {
                if (!f.Contains(ign))
                {
                    categories_.Add(f.Substring(f.LastIndexOf(Path.DirectorySeparatorChar)+1));
                }
            }
        }
        //
        if (param_.initialization)
        {
            if (param_.init_dir_list.Count>0)
            {
                foreach (string init_dir in param_.init_dir_list)
                {
                    //
                    cleanOutput(Path.Join(output_fdir_, init_dir));
                    //
                    generateOutput(init_dir);
                }
            }
            else
            { // init_dir_list is empty. Thus initialize all directories.
                //
                cleanOutput(output_fdir_);
                //
                generateOutput("");
            }
        }
        //
        string index_fpath = Path.Combine(output_fdir_, "index.html");
        generateIndexHtml(index_fpath);
    }
    //
    [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
    private static void Run()
    {
        // Check if the src_directory exists
        if (!Directory.Exists(src_fdir_)) {
            Console.WriteLine($"{src_fdir_} does not exist in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        // Create a new FileSystemWatcher and set its properties.
        using (FileSystemWatcher watcher = new FileSystemWatcher())
        {
            watcher.Path = src_fdir_;
            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;

            // Only watch text files.
            // watcher.Filter = "*.txt";
            //
            watcher.IncludeSubdirectories = true;
            // Add event handlers.
            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            // Begin watching.
            watcher.EnableRaisingEvents = true;
            // Wait for the user to quit the program.
            Console.WriteLine("Press 'q' to quit the sample.");
            while (Console.Read() != 'q') ;
        }
    }

    // Define the event handlers.
    private static void OnCreated(object source, FileSystemEventArgs e)
    {
        // Ignore the folders specified by the user
        // ignoreFolders(e.FullPath);
        // Specify what is done when a file is changed, created, or deleted.
        // Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
        //
        processAll(e.FullPath);
    }
// 
    private static void OnChanged(object source, FileSystemEventArgs e)
    {
        // Ignore the folders specified by the user
        // ignoreFolders(e.FullPath);
        // Specify what is done when a file is changed, created, or deleted.
        // Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
        //
        processAll(e.FullPath);
    }

    private static void OnDeleted(object source, FileSystemEventArgs e)
    {
        // Ignore the folders specified by the user
        // ignoreFolders(e.FullPath);
        // Specify what is done when a file is changed, created, or deleted.
        // Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
        // TODO: delete the corresponding file
        processDeleted(e.FullPath);
    }

    private static void OnRenamed(object source, RenamedEventArgs e)
    {
        // Ignore the folders specified by the user
        // ignoreFolders(e.FullPath);
        // Specify what is done when a file is renamed.
        // Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
        //
        processAllRenamed(e.FullPath, e.OldFullPath);
    }
    //
    // private static void ignoreFolders(string fpath)
    // {
    //     // Ignore the folders specified by the user
    //     foreach (var walk_hidden in param_.search_hide_dir_list)
    //     {
    //         if (fpath.Contains(walk_hidden))
    //         {
    //             return;
    //         }
    //     }
    // }
    //
    private static void processAll(string src_fpath)
    {
        if (markdown_ext_list.Contains(Path.GetExtension(src_fpath)))
        {
            processMarkdown(src_fpath);
        }
        else if (Path.GetExtension(src_fpath) == string.Empty)
        { // directory.
        // Do nothing.
            return;
        }
        else
        {
            processAttachment(src_fpath);
        }
    }
    //
    private static void processMarkdown(string md_fpath)
    {
        //
        string output_fpath = md_fpath.Replace("src","html");
        foreach (var md_ext in markdown_ext_list)
        {
            if (output_fpath.Contains(md_ext))
            {
                output_fpath = output_fpath.Replace(md_ext,".html");
            }
        }
        //
        string output_fdir = Path.GetDirectoryName(output_fpath);
        if (!Directory.Exists(output_fdir))
        {
            DirectoryInfo di = Directory.CreateDirectory(output_fdir);
        }
        //
        string tpl_fpath = Path.GetFullPath(Path.Join(cwd_,param_.pandoc_html_template));
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        //
        Process process = new Process();
        // Configure the process using the StartInfo properties.
        process.StartInfo.FileName = param_.pandoc_exec;
        process.StartInfo.Arguments = $"-f markdown -t html --template={tpl_fpath} " +
            "--mathjax --number-sections --number-offset=0 --toc --standalone --highlight-style=haddock " +
            $"--variable date={today} -o {output_fpath} {md_fpath}";
        // process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
        // Console.WriteLine(process.StartInfo.Arguments);
        process.Start();
        // Waits here for the process to exit.
        process.WaitForExit();
        //
        if (File.Exists(output_fpath))
        {
            Console.WriteLine($"{output_fpath} is generated.");
        }
        else
        {
            Console.WriteLine($"WARNING: {output_fpath} fails to be generated!");
        }
    }
    //
    private static void processAttachment(string src_fpath)
    {
        //
        foreach (string avoid_ext in param_.avoid_copy_ext_list)
        {
            if (src_fpath.Contains(avoid_ext))
            {
                return;
            }
        }
        //
        string output_fpath = src_fpath.Replace("src","html");
        //
        string output_fdir = Path.GetDirectoryName(output_fpath);
        if (!Directory.Exists(output_fdir))
        {
            DirectoryInfo di = Directory.CreateDirectory(output_fdir);
        }
        //
        for (int i=1; i <= n_retry_file; ++i) {
            try {
                //
                File.Copy(src_fpath, output_fpath, true);
                break;
            }
            catch (IOException e) when (i <= n_retry_file) {
                // You may check error code to filter some exceptions, not every error
                // can be recovered.
                Thread.Sleep(delay_retry);
            }
        }
        //
        //
        if (File.Exists(output_fpath))
        {
            Console.WriteLine($"{src_fpath} is copied to {output_fpath}.");
        }
        else
        {
            Console.WriteLine($"WARNING: {src_fpath} fails to be copied to {output_fpath} in {MethodBase.GetCurrentMethod().Name}!");
        }
    }
    //
    private static void processDeleted(string src_fpath)
    {
        string output_fpath = src_fpath.Replace("src","html");
        if (File.Exists(output_fpath))
        {
            File.Delete(output_fpath);
        }
    }
    //
    private static void processAllRenamed(string src_new_fpath, string src_old_fpath)
    {
        //
        string ext_new = Path.GetExtension(src_new_fpath);
        string ext_old = Path.GetExtension(src_old_fpath);
        if (String.Compare(ext_new, ext_old) != 0)
        {
            Console.WriteLine($"{src_old_fpath} is renamed to {src_new_fpath} with different extension in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        //
        if (markdown_ext_list.Contains(ext_new))
        {
            processMarkdownRenamed(src_new_fpath, src_old_fpath);
        }
        else if (src_new_fpath == string.Empty)
        { // directory
            processDirectoryRenamed(src_new_fpath, src_old_fpath);
        }
        else
        {
            processAttachmentRenamed(src_new_fpath, src_old_fpath);
        }
    }
    //
    private static void processDirectoryRenamed(string src_new_dir, string src_old_dir)
    {
        string output_new_dir = src_new_dir.Replace("src","html");
        string output_old_dir = src_old_dir.Replace("src","html");
        if (!Directory.Exists(output_old_dir))
        {
            Console.WriteLine($"{output_old_dir} does not exist to be moved to {output_new_dir} in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        if (Directory.Exists(output_new_dir))
        {
            Console.WriteLine($"{output_new_dir} already exists to be moved from {output_old_dir} in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        Directory.Move(output_old_dir, output_new_dir);
    }
    //
    private static void processMarkdownRenamed(string src_new_fpath, string src_old_fpath)
    {
        //
        string output_new_fpath = src_new_fpath.Replace("src","html");
        string output_old_fpath = src_old_fpath.Replace("src","html");
        foreach (var md_ext in markdown_ext_list)
        {
            if (output_new_fpath.Contains(md_ext))
            {
                output_new_fpath.Replace(md_ext,".html");
            }
        }
        foreach (var md_ext in markdown_ext_list)
        {
            if (output_old_fpath.Contains(md_ext))
            {
                output_old_fpath.Replace(md_ext,".html");
            }
        }
        //
        if (!File.Exists(output_old_fpath))
        {
            Console.WriteLine($"{output_old_fpath} does not exist to be moved to {output_new_fpath} in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        if (File.Exists(output_new_fpath))
        {
            Console.WriteLine($"{output_new_fpath} already exists to be moved from {output_old_fpath} in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        File.Move(output_old_fpath, output_new_fpath, false);
    }
    //
    private static void processAttachmentRenamed(string src_new_fpath, string src_old_fpath)
    {
        //
        string output_new_fpath = src_new_fpath.Replace("src","html");
        string output_old_fpath = src_old_fpath.Replace("src","html");
        //
        if (!File.Exists(output_old_fpath))
        {
            Console.WriteLine($"{output_old_fpath} does not exist to be moved to {output_new_fpath} in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        if (File.Exists(output_new_fpath))
        {
            Console.WriteLine($"{output_new_fpath} already exists to be moved from {output_old_fpath} in {MethodBase.GetCurrentMethod().Name}!");
            Environment.Exit(-1);
        }
        File.Move(output_old_fpath, output_new_fpath, false);
        Console.WriteLine($"{output_old_fpath} is moved to {output_new_fpath} in {MethodBase.GetCurrentMethod().Name}.");
    }
    //
    private static void cleanOutput(string target_fdir)
    {
        if (Directory.Exists(target_fdir))
        {
            Directory.Delete(target_fdir, true);
        }
        DirectoryInfo di = Directory.CreateDirectory(target_fdir);
    }
    //
    private static void generateOutput(string init_dir)
    {
        //
        if (String.IsNullOrEmpty(init_dir))
        {
            //
            foreach (string cat in categories_)
            {
                string cat_fdir = Path.Join(src_fdir_, cat);
                loopDirForOutput(cat_fdir);
            }
        }
        else
        {
            string init_abs_dir = Path.Join(src_fdir_, init_dir);
            loopDirForOutput(init_abs_dir);
        }
    }
    //
    private static void loopDirForOutput(string sDir)
    {
        try
        {
            //
            bool skip = false;
            foreach (string f in Directory.GetFiles(sDir))
            {
                foreach (var ign in param_.search_hide_dir_list)
                {
                    if (f.Contains(ign))
                    {
                        skip = true;
                    }
                }
                //
                if (!skip)
                {
                    processAll(f);
                }
            }
            //
            foreach (string d in Directory.GetDirectories(sDir))
            {
                //
                skip = false;
                string d_name = d.Substring(d.LastIndexOf(Path.DirectorySeparatorChar)+1);
                foreach (string hide in param_.tree_hide_dir_list)
                {
                    if (String.Compare(d_name,hide)==0)
                    {
                        skip = true;
                    }
                }
                //
                if (!skip)
                {
                    loopDirForOutput(d);
                }
            }
        }
        catch (System.Exception excpt)
        {
            Console.WriteLine(excpt.Message);
        }
    }
    //
    private static void generateIndexHtml(string index_fpath)
    {
        if (File.Exists(index_fpath))
        {
            File.Delete(index_fpath);
        }
        //
        // var files = Directory.EnumerateFiles(output_fdir_, "*.html", SearchOption.AllDirectories);
        // foreach (var f in files)
        // {
        //     Console.WriteLine($"{f}");
        // }
        // Console.WriteLine($"{files.Count().ToString()} files found.");
        //
        StreamReader index_template_file = File.OpenText(Path.GetFullPath(Path.Join(cwd_,param_.index_html_template)));
        string index_template_content = index_template_file.ReadToEnd();
        index_template_file.Close();
        List<string> item_list = new List<string>();
        int counter = 0;
        foreach (string cat in categories_)
        {
            // index_file.WriteLine("<h1>"+cat+"</h1>");
            item_list.Add("<h1>"+cat+"</h1>");
            //
            string cat_fdir = Path.Join(output_fdir_, cat);
            int level_base = 2;
            loopDirForGenHtml(cat_fdir, item_list, level_base, ref counter);
        }
        //
        StreamWriter index_file = File.CreateText(index_fpath);
        string index_content = index_template_content.Replace("CONTENT_TO_BE_REPLACED", String.Join("\n", item_list));
        index_file.WriteLine(index_content);
        index_file.Close();
    }
    //
    private static void loopDirForGenHtml(string sDir, List<string> item_list, int level, ref int counter)
    {
        try
        {
            //
            counter += 1;
            //
            foreach (string f in Directory.GetFiles(sDir))
            {
                foreach (string ext in param_.tree_show_file_ext_list)
                {
                    if (String.Compare(Path.GetExtension(f),"."+ext)==0)
                    {
                        appendItem(f, item_list);
                    }
                }
            }
            //
            foreach (string d in Directory.GetDirectories(sDir))
            {
                //
                bool show = true;
                string d_name = d.Substring(d.LastIndexOf(Path.DirectorySeparatorChar)+1);
                foreach (string hide in param_.tree_hide_dir_list)
                {
                    if (String.Compare(d_name,hide)==0)
                    {
                        show = false;
                    }
                }
                //
                if (show)
                {
                    // item_list.Add($"<button value=\"{counter}\" onclick=\"toggleSection(this.value)\">+</button>");
                    item_list.Add($"<ul>");
                    string this_dir = d.Substring(d.LastIndexOf(Path.DirectorySeparatorChar)+1);
                    item_list.Add($"<h{level}><input type=\"button\" id=\"{counter}\" value=\"➕\" onclick=\"toggleSection(this.id)\"></button>{this_dir}</h{level}>");
                    item_list.Add($"<div id=\"section-{counter}\" style=\"display: none\">");
                    loopDirForGenHtml(d, item_list, level+1, ref counter);
                    item_list.Add("</div>");
                    item_list.Add("</ul>");
                }
            }
        }
        catch (System.Exception excpt)
        {
            Console.WriteLine(excpt.Message);
        }
    }
    //
    private static void appendItem(string abs_f, List<string> item_list)
    {
        string rel_f = abs_f.Remove(abs_f.IndexOf(output_fdir_), output_fdir_.Length+1);
        string item_link = rel_f;
        string item_text = rel_f.Substring(rel_f.LastIndexOf(Path.DirectorySeparatorChar)+1);
        item_list.Add($"<li><a href=\"{item_link}\" target=\"_blank\"><span class=\"link_text\">{item_text}</span></a></li><br>");
        // //
        // var idx = rel_f.IndexOf(Path.DirectorySeparatorChar);
        // if (idx<0)
        // { // It has no directory. Probably it is index.html.
        //     return;
        // }
        // string top_dir = rel_f.Remove(idx);
        // if ( String.Compare(top_dir, cat) == 0 )
        // {
        //     string item_link = rel_f;
        //     string item_text = rel_f.Substring(rel_f.LastIndexOf(Path.DirectorySeparatorChar)+1);
        //     // index_file.WriteLine($"<li><a href=\"{item_link}\"><span class=\"link_text\">{item_text}</span></a></li><br>");
        //     // String.Concat(, );
        // }
    }
}