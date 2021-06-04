using Urho;
using System;
using Urho.IO;
using Urho.Resources;

public static  class PlayerPrefs
{
    static Application application = null;
    static string applicationName = "";

    static string filePath = "";
    public static void Init(Application app)
    {
        application = app;

        // Application Type = NameSpace.ClassName
        applicationName = application.GetType().ToString().Split('.')[1];

        string dirPath = (application.FileSystem.UserDocumentsDir + "/" + applicationName).Replace(@"\", @"/").Replace(@"//", @"/");
        filePath = dirPath + "/PlayerPrefs.xml";

        application.FileSystem.CreateDir(dirPath);
        if(application.FileSystem.FileExists(filePath) == false)
        {
            using (var file = new File(application.Context, filePath, FileMode.Write))
            {
                var xmlConfig = new XmlFile();
                var configElem = xmlConfig.CreateRoot("root");
                xmlConfig.Save(file);
                file.Close();
            }
        }
    }


    private static XmlFile GetXmlConfig()
    {
        var xmlConfig = new XmlFile();
        using (var file = new File(application.Context, filePath, FileMode.Read))
        {
            xmlConfig.Load(file);
        }

        return xmlConfig;
    }

    private static void SaveXmlConfig(XmlFile xmlConfig)
    {
        using (var file = new File(application.Context, filePath, FileMode.Write))
        {
            xmlConfig.Save(file);
            file.Close();
        }
    }

    public static bool HasKey(string key)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return false;

        return true;
    }



    public static void DeleteAll()
    {
        using (var file = new File(application.Context, filePath, FileMode.Write))
        {
            var xmlConfig = new XmlFile();
            var configElem = xmlConfig.CreateRoot("root");
            xmlConfig.Save(file);
            file.Close();
        }
    }

    private static string RemoveWhitespace(string str)
    {
        return str.Replace(@" ", @"_");
    }

    public static bool SetString(string key, string value)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetOrCreateChild(key);
        if (childElem.Null == true) return false;

        if (childElem.SetString("value", value) == false) return false;

        SaveXmlConfig(xmlConfig);

        return true;
    }

    public static string GetString(string key, string defaultValue = "")
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return defaultValue;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return defaultValue;

        return childElem.GetAttribute("value");

    }

    
    public static bool SetInt(string key, int value)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetOrCreateChild(key);
        if (childElem.Null == true) return false;

        if (childElem.SetInt("value", value) == false) return false;

        SaveXmlConfig(xmlConfig);

        return true;
    }

    public static int  GetInt(string key, int defaultValue = 0)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return defaultValue;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return defaultValue;

        
        return childElem.GetInt("value");
    }

    ///
    public static bool SetInt64(string key, System.Int64 value)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetOrCreateChild(key);
        if (childElem.Null == true) return false;

        if (childElem.SetInt64("value", value) == false) return false;

        SaveXmlConfig(xmlConfig);

        return true;
    }

    public static System.Int64 GetInt64(string key, System.Int64 defaultValue = 0)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return defaultValue;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return defaultValue;

        
        return childElem.GetInt64("value");
    }

    public static bool SetFloat(string key, float value)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetOrCreateChild(key);
        if (childElem.Null == true) return false;

        if (childElem.SetFloat("value", value) == false) return false;

        SaveXmlConfig(xmlConfig);

        return true;
    }

    public static float GetFloat(string key , float defaultValue = 0.0f)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return defaultValue;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return defaultValue;

        return childElem.GetFloat("value");
    }

    ///////////////////////////////////////////////////////////////////

        public static bool SetDouble(string key, double value)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetOrCreateChild(key);
        if (childElem.Null == true) return false;

        if (childElem.SetDouble("value", value) == false) return false;

        SaveXmlConfig(xmlConfig);

        return true;
    }

    public static double GetDouble(string key , double defaultValue = 0.0)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return defaultValue;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return defaultValue;

        return childElem.GetDouble("value");
    }


    public static bool SetBool(string key, bool value)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetOrCreateChild(key);
        if (childElem.Null == true) return false;

        if (childElem.SetBool("value", value) == false) return false;

        SaveXmlConfig(xmlConfig);

        return true;
    }

    public static bool GetBool(string key , bool defaultValue = false)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return defaultValue;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return defaultValue;

        return childElem.GetBool("value");
    }


    public static bool DeleteKey(string key)
    {
        var xmlConfig = GetXmlConfig();
        key = RemoveWhitespace(key);

        XmlElement rootElem = xmlConfig.GetRoot();
        if (rootElem.Null == true) return false;

        XmlElement childElem = rootElem.GetChild(key);
        if (childElem.Null == true) return false;

        rootElem.RemoveChild(childElem);

        SaveXmlConfig(xmlConfig);


        return true;
    }
}