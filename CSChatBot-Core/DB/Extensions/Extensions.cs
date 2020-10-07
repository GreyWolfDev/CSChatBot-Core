using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DB.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = DB.Models.User;

namespace DB.Extensions
{
    public static class Extensions
    {
        #region Users

        public static void Save(this User u, Instance db)
        {
            if (u.ID == null || !ExistsInDb(u, db))
            {
                //need to insert
                db.ExecuteNonQuery(
                    "insert into users (Name, UserId, UserName, FirstSeen, LastHeard, Points, Location, Debt, LastState, Greeting, Grounded, GroundedBy, IsBotAdmin, LinkingKey, Description) VALUES (@Name, @UserId, @UserName, @FirstSeen, @LastHeard, @Points, @Location, @Debt, @LastState, @Greeting, @Grounded, @GroundedBy, @IsBotAdmin, @LinkingKey, @Description)",
                    u);
                u.ID =
                    db.Connection.Query<int>(
                        $"SELECT ID FROM Users WHERE UserId = @UserId", u)
                        .First();
            }
            else
            {
                db.ExecuteNonQuery(
                    "UPDATE users SET Name = @Name, UserId = @UserId, UserName = @UserName, FirstSeen = @FirstSeen, LastHeard = @LastHeard, Points = @Points, Location = @Location, Debt = @Debt, LastState = @LastState, Greeting = @Greeting, Grounded = @Grounded, GroundedBy = @GroundedBy, IsBotAdmin = @IsBotAdmin, LinkingKey = @LinkingKey, Description = @Description WHERE ID = @ID",
                    u);
            }
        }

        public static bool ExistsInDb(this User user, Instance db)
        {
            var rows = db.Connection.Query("SELECT COUNT(1) as 'Count' FROM Users WHERE ID = @ID", user);
            return (int)rows.First().Count > 0;
        }

        public static void RemoveFromDb(this User user, Instance db)
        {
            db.ExecuteNonQuery("DELETE FROM Users WHERE ID = @ID", user);
        }

        /// <summary>
        /// Gets a user setting from the database
        /// </summary>
        /// <typeparam name="T">The type the setting should be (bool, int, string)</typeparam>
        /// <param name="user">What user the setting comes from</param>
        /// <param name="field">The name of the setting</param>
        /// <param name="db">The database instance</param>
        /// <param name="def">The default value for the field</param>
        /// <returns></returns>
        public static T GetSetting<T>(this User user, string field, Instance db, T def)
        {
            if (db.Connection.State != ConnectionState.Open)
                db.Connection.Open();
            //verify settings exist
            var columns = new SQLiteCommand("PRAGMA table_info(users)", db.Connection).ExecuteReader();
            var t = default(T);

            while (columns.Read())
            {
                if (String.Equals(columns[1].ToString(), field))
                {
                    var result = db.Connection.ExecuteScalar($"select [{field}] from users where ID = @ID", new { user.ID });
                    if (t != null)
                    {
                        if (t.GetType() == typeof(bool))
                            result = result.ToString() == "1"; // convert to boolean
                        else if (t.GetType() == typeof(int))
                            result = Convert.ToInt32((long)result); // convert int64 to int32
                    }
                    return (T)result;
                }
            }
            var type = "BLOB";
            if (t == null)
                type = "TEXT";
            else if (t.GetType() == typeof(int))
                type = "INTEGER";
            else if (t.GetType() == typeof(bool))
                type = "INTEGER";

            var d = def.FormatSQL();
            db.ExecuteNonQuery($"ALTER TABLE users ADD COLUMN [{field}] {type} DEFAULT {d}");
            return def;
        }

        /// <summary>
        /// Sets a user setting to the database
        /// </summary>
        /// <typeparam name="T">The type the setting should be (bool, int, string)</typeparam>
        /// <param name="user">What user the setting comes from</param>
        /// <param name="field">The name of the setting</param>
        /// <param name="db">The database instance</param>
        /// <param name="def">The default value for the field</param>
        /// <returns></returns>
        public static bool SetSetting<T>(this User user, string field, Instance db, T def, T value)
        {
            try
            {
                if (db.Connection.State != ConnectionState.Open)
                    db.Connection.Open();
                //verify settings exist
                var columns = new SQLiteCommand("PRAGMA table_info(users)", db.Connection).ExecuteReader();
                var t = default(T);
                var type = "BLOB";
                if (t == null)
                    type = "TEXT";
                else if (t.GetType() == typeof(int))
                    type = "INTEGER";
                else if (t.GetType() == typeof(bool))
                    type = "INTEGER";
                bool settingExists = false;
                while (columns.Read())
                {
                    if (String.Equals(columns[1].ToString(), field))
                    {
                        settingExists = true;
                    }
                }
                if (!settingExists)
                {
                    var d = def.FormatSQL();
                    db.ExecuteNonQuery($"ALTER TABLE users ADD COLUMN [{field}] {type} DEFAULT {d}");
                }

                db.ExecuteNonQuery($"UPDATE users set [{field}] = @value where ID = @ID", new { value, user.ID });

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Groups

        public static void Save(this Group g, Instance db)
        {
            if (g.ID == null || !ExistsInDb(g, db))
            {
                //need to insert
                db.ExecuteNonQuery(
                    "insert into chatgroup (GroupId, Name, UserName, MemberCount) VALUES (@GroupId, @Name, @UserName, @MemberCount)",
                    g);
                g.ID =
                    db.Connection.Query<int>(
                        $"SELECT ID FROM chatgroup WHERE GroupId = @GroupId", g)
                        .First();
            }
            else
            {
                db.ExecuteNonQuery(
                    "UPDATE chatgroup SET GroupId = @GroupId, Name = @Name, UserName = @UserName, MemberCount = @MemberCount WHERE ID = @ID",
                    g);
            }
        }

        public static bool ExistsInDb(this Group group, Instance db)
        {
            var rows = db.Connection.Query($"SELECT COUNT(1) as 'Count' FROM chatgroup WHERE ID = @ID", group);
            return (int)rows.First().Count > 0;
        }

        public static void RemoveFromDb(this Group group, Instance db)
        {
            db.ExecuteNonQuery("DELETE FROM chatgroup WHERE ID = @ID", group);
        }

        /// <summary>
        /// Gets a group setting from the database
        /// </summary>
        /// <typeparam name="T">The type the setting should be (bool, int, string)</typeparam>
        /// <param name="group">What group the setting comes from</param>
        /// <param name="field">The name of the setting</param>
        /// <param name="db">The database instance</param>
        /// <param name="def">The default value for the field</param>
        /// <returns></returns>
        public static T GetSetting<T>(this Group group, string field, Instance db, T def)
        {
            if (db.Connection.State != ConnectionState.Open)
                db.Connection.Open();
            //verify settings exist
            var columns = new SQLiteCommand("PRAGMA table_info(chatgroup)", db.Connection).ExecuteReader();
            var t = default(T);
            while (columns.Read())
            {
                if (String.Equals(columns[1].ToString(), field))
                {
                    var result = db.Connection.ExecuteScalar($"select [{field}] from chatgroup where ID = @ID", new { group.ID });
                    if (t != null)
                    {
                        if (t.GetType() == typeof(bool))
                            result = result.ToString() == "1"; // convert to boolean
                        else if (t.GetType() == typeof(int))
                            result = Convert.ToInt32((long)result); // convert int64 to int32
                    }
                    return (T)result;
                }
            }
            var type = "BLOB";
            if (t == null)
                type = "TEXT";
            else if (t.GetType() == typeof(int))
                type = "INTEGER";
            else if (t.GetType() == typeof(bool))
                type = "INTEGER";

            var d = def.FormatSQL();
            db.ExecuteNonQuery($"ALTER TABLE chatgroup ADD [{field}] {type} DEFAULT {d}");
            return def;
        }

        /// <summary>
        /// Gets a group setting from the database
        /// </summary>
        /// <typeparam name="T">The type the setting should be (bool, int, string)</typeparam>
        /// <param name="group">What group the setting comes from</param>
        /// <param name="field">The name of the setting</param>
        /// <param name="db">The database instance</param>
        /// <param name="def">The default value for the field</param>
        /// <returns></returns>
        public static bool SetSetting<T>(this Group group, string field, Instance db, T def, T value)
        {
            try
            {
                if (db.Connection.State != ConnectionState.Open)
                    db.Connection.Open();
                //verify settings exist
                var columns = new SQLiteCommand("PRAGMA table_info(chatgroup)", db.Connection).ExecuteReader();
                var t = default(T);
                var type = "BLOB";
                if (t == null)
                    type = "TEXT";
                else if (t.GetType() == typeof(int))
                    type = "INTEGER";
                else if (t.GetType() == typeof(bool))
                    type = "INTEGER";
                bool settingExists = false;
                while (columns.Read())
                {
                    if (String.Equals(columns[1].ToString(), field))
                    {
                        settingExists = true;
                    }
                }
                if (!settingExists)
                {
                    var d = def.FormatSQL();
                    db.ExecuteNonQuery($"ALTER TABLE chatgroup ADD [{field}] {type} DEFAULT {d}");
                }

                db.ExecuteNonQuery($"UPDATE chatgroup set [{field}] = @value WHERE ID = @ID", new { value, group.ID });

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Settings

        public static void Save(this Setting set, Instance db)
        {
            if (set.ID == null || !ExistsInDb(set, db))
            {
                //need to insert
                db.ExecuteNonQuery(
                    "insert into settings (Alias, TelegramBotAPIKey, TelegramDefaultAdminUserId) VALUES (@Alias, @TelegramBotAPIKey, @TelegramDefaultAdminUserId)",
                    set);
                set.ID = db.Connection.Query<int>("SELECT ID FROM Settings WHERE Alias = @Alias", set).First();
            }
            else
            {
                db.ExecuteNonQuery(
                    "UPDATE settings SET Alias = @Alias, TelegramBotAPIKey = @TelegramBotAPIKey, TelegramDefaultAdminUserId = @TelegramDefaultAdminUserId WHERE ID = @ID",
                    set);
            }
        }

        public static bool ExistsInDb(this Setting set, Instance db)
        {
            var rows = db.Connection.Query($"SELECT COUNT(1) as 'Count' FROM settings WHERE ID = @ID", set);
            return (int)rows.First().Count > 0;
        }

        public static void RemoveFromDb(this Setting set, Instance db)
        {
            db.ExecuteNonQuery("DELETE FROM settings WHERE ID = @ID", set);
        }

        /// <summary>
        /// Gets a setting from the database
        /// </summary>
        /// <typeparam name="T">The type the setting should be (bool, int, string)</typeparam>
        /// <param name="set">The setting to read from</param>
        /// <param name="field">The name of the setting</param>
        /// <param name="db">The database instance</param>
        /// <param name="def">The default value for the field</param>
        /// <returns></returns>
        public static T GetSetting<T>(this Setting set, string field, Instance db, T def)
        {
            if (db.Connection.State != ConnectionState.Open)
                db.Connection.Open();
            //verify settings exist
            var columns = new SQLiteCommand("PRAGMA table_info(settings)", db.Connection).ExecuteReader();
            var t = default(T);
            while (columns.Read())
            {
                if (String.Equals(columns[1].ToString(), field))
                {
                    var result = db.Connection.ExecuteScalar($"select [{field}] from settings where ID = @ID", new { set.ID });
                    if (t != null)
                    {
                        if (t.GetType() == typeof(bool))
                            result = result.ToString() == "1"; // convert to boolean
                        else if (t.GetType() == typeof(int))
                            result = Convert.ToInt32((long)result); // convert int64 to int32
                    }
                    return (T)result;
                }
            }
            var type = "BLOB";
            if (t == null)
                type = "TEXT";
            else if (t.GetType() == typeof(int))
                type = "INTEGER";
            else if (t.GetType() == typeof(bool))
                type = "INTEGER";

            var d = def.FormatSQL();
            db.ExecuteNonQuery($"ALTER TABLE settings ADD [{field}] {type} DEFAULT {d}");
            return (T)def;
        }

        /// <summary>
        /// Writes a setting to the database
        /// </summary>
        /// <typeparam name="T">The type the setting should be (bool, int, string)</typeparam>
        /// <param name="set">The setting to be edited</param>
        /// <param name="field">The name of the setting</param>
        /// <param name="db">The database instance</param>
        /// <param name="def">The default value for the field</param>
        /// <returns></returns>
        public static bool SetSetting<T>(this Setting set, string field, Instance db, T def, T value)
        {
            try
            {
                if (db.Connection.State != ConnectionState.Open)
                    db.Connection.Open();
                //verify settings exist
                var columns = new SQLiteCommand("PRAGMA table_info(settings)", db.Connection).ExecuteReader();
                var t = default(T);
                var type = "BLOB";
                if (t == null)
                    type = "TEXT";
                else if (t.GetType() == typeof(int))
                    type = "INTEGER";
                else if (t.GetType() == typeof(bool))
                    type = "INTEGER";
                bool settingExists = false;
                while (columns.Read())
                {
                    if (String.Equals(columns[1].ToString(), field))
                    {
                        settingExists = true;
                    }
                }
                if (!settingExists)
                {
                    var d = def.FormatSQL();
                    db.ExecuteNonQuery($"ALTER TABLE settings ADD [{field}] {type} DEFAULT {d}");
                }

                db.ExecuteNonQuery($"UPDATE settings set [{field}] = @value WHERE ID = @ID", new { value, set.ID });

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        public static string ExecuteQuery(this Instance db, string commandText, object param = null)
        {
            // Ensure we have a connection
            if (db.Connection == null)
            {
                throw new NullReferenceException(
                    "Please provide a connection");
            }

            // Ensure that the connection state is Open
            if (db.Connection.State != ConnectionState.Open)
            {
                db.Connection.Open();
            }
            var reader = db.Connection.ExecuteReader(commandText, param);
            var response = "";
            for (int i = 0; i < reader.FieldCount; i++)
            {
                response += $"{reader.GetName(i)} - ";
            }
            response += "\n";
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    response += $"{reader[i]} - ";
                }
                response += "\n";
            }
            // Use Dapper to execute the given query
            return response;
        }

        public static int ExecuteNonQuery(this Instance db, string commandText, object param = null)
        {
            // Ensure we have a connection
            if (db.Connection == null)
            {
                throw new NullReferenceException(
                    "Please provide a connection");
            }

            // Ensure that the connection state is Open
            if (db.Connection.State != ConnectionState.Open)
            {
                db.Connection.Open();
            }

            // Use Dapper to execute the given query
            return db.Connection.Execute(commandText, param);
        }

        public static string ToString(this Telegram.Bot.Types.User user)
        {
            return (user.FirstName + " " + user.LastName).Trim();
        }

        #region Helpers
        public static User GetTarget(this Message message, string args, User sourceUser, Instance db)
        {
            if (message == null) return sourceUser;
            if (message?.ReplyToMessage != null)
            {
                var m = message.ReplyToMessage;
                var userid = m.ForwardFrom?.Id ?? m.From.Id;
                return db.Users.FirstOrDefault(x => x.UserId == userid) ?? sourceUser;
            }
            if (String.IsNullOrWhiteSpace(args))
            {
                return sourceUser;
            }
            //check for a user mention
            var mention = message?.Entities.FirstOrDefault(x => x.Type == MessageEntityType.Mention);
            var textmention = message?.Entities.FirstOrDefault(x => x.Type == MessageEntityType.TextMention);
            var id = 0;
            var username = "";
            if (mention != null)
                username = message.Text.Substring(mention.Offset + 1, mention.Length - 1);
            else if (textmention != null)
            {
                id = textmention.User.Id;
            }
            User result = null;
            if (!String.IsNullOrEmpty(username))
                result = db.Users.FirstOrDefault(
                    x =>
                        String.Equals(x.UserName, username,
                            StringComparison.InvariantCultureIgnoreCase));
            else if (id != 0)
                result = db.Users.FirstOrDefault(x => x.UserId == id);
            else
                result = db.Users.FirstOrDefault(
                        x =>
                            String.Equals(x.UserId.ToString(), args, StringComparison.InvariantCultureIgnoreCase) ||
                            String.Equals(x.UserName, args.Replace("@", ""), StringComparison.InvariantCultureIgnoreCase));
            return result ?? sourceUser;
        }

        /// <summary>
        /// Prepare an object for use in SQL query
        /// </summary>
        /// <param name="o">The object that's supposed to be used in the SQL query</param>
        /// <returns>The escaped <paramref name="o"/> that is safe for use in SQL queries</returns>
        public static string FormatSQL(this object o)
        {
            if (o == null) return "null";
            if (o.GetType() == typeof(bool)) return (bool)o ? "1" : "0";
            if (o.GetType() == typeof(int) || o.GetType() == typeof(long)) return o.ToString();

            return $"'{o.ToString().Replace("'", "''").Replace("\"", "\"\"")}'";
        }
        #endregion
    }
}
