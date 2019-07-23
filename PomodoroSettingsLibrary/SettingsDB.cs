using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PomodoroSettingsLibrary;
using System.Collections.ObjectModel;

namespace PomodoroSettingsLibrary
{
    public static class SettingsDB
    {
        static string sqlConnectionFileName = "Filename=SettingsPresets.db";
        static string themeFileName = "Filename=ThemePreference.db";

        public static void Initialize_DB()
        {
            using (SqliteConnection db =
                new SqliteConnection(sqlConnectionFileName))
            {
                db.Open();

                String initializeTableCommand = "CREATE TABLE " +
                    "IF NOT EXISTS SettingsPresetsTable " +
                    "(presetName TEXT PRIMARY KEY NOT NULL, " +
                    "sessionTime TEXT NOT NULL, " +
                    "shortBreakTime TEXT NOT NULL, " +
                    "longBreakTime TEXT NOT NULL) ";

                using (SqliteCommand createTable = new SqliteCommand(initializeTableCommand, db))
                {
                    createTable.ExecuteNonQuery();
                }

                db.Close();
            }
        }

        public static void DB_Insert_PRESET(PresetSettings aSettings)
        {
            using (SqliteConnection db =
                new SqliteConnection(sqlConnectionFileName))
            {
                db.Open();

                String insertPresetCommand = "INSERT INTO "+
                    "SettingsPresetsTable "+
                    "(presetName, sessionTime, " +
                    "shortBreakTime, longBreakTime) "+
                    "VALUES "+
                    "('"+aSettings.GetPresetName()+"', '"+ aSettings.GetSessionTime().ToString("mm\\:ss")+ "', '"
                    + aSettings.GetShortBreakTime().ToString("mm\\:ss") + "', '"+aSettings.GetLongBreakTime().ToString("mm\\:ss") + "')";

                using (SqliteCommand insertPreset = new SqliteCommand(insertPresetCommand, db))
                {
                    try
                    {
                        insertPreset.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        //Sql Exception. Preset already exists...
                        db.Close();
                    }
                }
                db.Close();
            }
        }

        public static PresetSettings DB_Select_PRESET(string presetName)
        {
            using (SqliteConnection db =
                new SqliteConnection(sqlConnectionFileName))
            {
                db.Open();

                String selectPresetCommand = "SELECT sessionTime, shortBreakTime, longBreakTime " +
                    "FROM SettingsPresetsTable " +
                    "WHERE presetName = '"+presetName+"'";

                using (SqliteCommand selectPreset = new SqliteCommand(selectPresetCommand, db))
                {
                    using (SqliteDataReader presetData = selectPreset.ExecuteReader())
                    {
                        presetData.Read();

                        PresetSettings returnSettings = new PresetSettings(presetName);
                        //Geee what a mess. Goodluck!
                        returnSettings.SetSessionTime(TimeSpan.ParseExact((string)presetData["sessionTime"], "mm\\:ss", new CultureInfo("en-US")));
                        returnSettings.SetShortBreakTime(TimeSpan.ParseExact((string)presetData["shortBreakTime"], "mm\\:ss", new CultureInfo("en-US")));
                        returnSettings.SetLongBreakTime(TimeSpan.ParseExact((string)presetData["longBreakTime"], "mm\\:ss", new CultureInfo("en-US")));
                        db.Close();
                        return returnSettings;
                    }
                }
            }
        }

        public static void DB_UPDATE_PRESET(PresetSettings aSettings)
        {
            using (SqliteConnection db =
                new SqliteConnection(sqlConnectionFileName))
            {
                db.Open();

                String updatePresetCommand = "UPDATE SettingsPresetsTable " +
                    "SET sessionTime = '" + aSettings.GetSessionTime().ToString("mm\\:ss") + "', " +
                    "shortBreakTime = '" + aSettings.GetShortBreakTime().ToString("mm\\:ss") + "', " +
                    "longBReakTime = '" + aSettings.GetLongBreakTime().ToString("mm\\:ss") + "' " +
                    "WHERE presetName = '" + aSettings.GetPresetName() + "' ";

                using (SqliteCommand updatePreset = new SqliteCommand(updatePresetCommand, db))
                {
                    updatePreset.ExecuteNonQuery();
                }
                db.Close();
            }
        }

        public static void DB_Delete_PRESET(string presetName)
        {
            using (SqliteConnection db =
                new SqliteConnection(sqlConnectionFileName))
            {
                db.Open();

                String deletePresetCommand = "DELETE FROM SettingsPresetsTable " +
                    "WHERE presetName LIKE '" + presetName + "' ";

                SqliteCommand deletePreset = new SqliteCommand(deletePresetCommand, db);

                deletePreset.ExecuteNonQuery();

                db.Close();
            }
        }

        public static ObservableCollection<string> DB_Retrieve_PRESET_AllNames()
        {
            //Finds all preset names in .db to be used in preset combobox
            //returns an itemsource of presetName

            using (SqliteConnection db =
                new SqliteConnection(sqlConnectionFileName))
            {
                db.Open();

                String selectPresetCommand = "SELECT presetName " +
                    "FROM SettingsPresetsTable ";

                ObservableCollection<string> aPresetList = new ObservableCollection<string>();

                using (SqliteCommand selectPreset = new SqliteCommand(selectPresetCommand, db))
                {
                    using (SqliteDataReader presetData = selectPreset.ExecuteReader())
                    {
                        while(presetData.Read())
                        {
                            aPresetList.Add((string)presetData["presetName"]);
                        }

                        db.Close();
                        return aPresetList;
                    }
                }
            }
        }

        public static void Initialize_Theme()
        {
            using (SqliteConnection db =
                new SqliteConnection(themeFileName))
            {
                db.Open();

                String initializeTableCommand = "CREATE TABLE " +
                    "IF NOT EXISTS ThemeTable " +
                    "(pk INT PRIMARY KEY NOT NULL, " +
                    "useDarkTheme TEXT NOT NULL) ";


                using (SqliteCommand createTable = new SqliteCommand(initializeTableCommand, db))
                {
                    createTable.ExecuteNonQuery();
                }

                db.Close();
            }
        }

        public static void DB_IfNotExistsCreate_USEDARKTHEME()
        {
            using (SqliteConnection db =
                new SqliteConnection(themeFileName))
            {
                db.Open();

                String selectCountThemeCommand = "SELECT pk " +
                   "FROM ThemeTable ";

                using (SqliteCommand selectTheme = new SqliteCommand(selectCountThemeCommand, db))
                {
                    using (SqliteDataReader themeData = selectTheme.ExecuteReader())
                    {
                        themeData.Read();

                        //No data exists in table..Insert data
                        if (!themeData.HasRows)
                        {
                            String insertThemeCommand = "INSERT INTO " +
                                 "ThemeTable " +
                                 "(pk, useDarkTheme) " +
                                 "VALUES " +
                                 "(0, 'True')";

                            using (SqliteCommand insertTheme = new SqliteCommand(insertThemeCommand, db))
                            {
                                try
                                {
                                    insertTheme.ExecuteNonQuery();
                                }
                                catch (Exception e)
                                {
                                    //Sql Exception. Preset already exists...
                                    db.Close();
                                }
                            }
                        }
                        else
                        {
                            //Data for theme already exists...
                        }
                    }
                }

                db.Close();
            }
        }

        public static bool DB_Select_USEDARKTHEME()
        {
            using (SqliteConnection db =
                new SqliteConnection(themeFileName))
            {
                db.Open();

                String selectThemeCommand = "SELECT useDarkTheme " +
                    "FROM ThemeTable " +
                    "WHERE pk = 0";

                using (SqliteCommand selectTheme = new SqliteCommand(selectThemeCommand, db))
                {
                    using (SqliteDataReader themeData = selectTheme.ExecuteReader())
                    {
                        themeData.Read();

                        string stringBool = themeData["useDarkTheme"].ToString();
                        bool returnBool = bool.Parse(stringBool);
                        db.Close();
                        return returnBool;
                    }
                }
            }
        }

        public static void DB_Update_USEDARKTHEME(bool passedBool)
        {
            using (SqliteConnection db =
                new SqliteConnection(themeFileName))
            {
                db.Open();

                String updatePresetCommand = "UPDATE ThemeTable " +
                    "SET useDarkTheme = '"+passedBool.ToString()+"' "+
                    "WHERE pk = 0 ";

                using (SqliteCommand updateTheme = new SqliteCommand(updatePresetCommand, db))
                {
                    updateTheme.ExecuteNonQuery();
                }
                db.Close();
            }
        }
    }
}
