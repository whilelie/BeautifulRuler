using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Windows.Forms;

namespace BeautifulRuler
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DataTable ExecuteQuery(string query)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        using (OracleDataAdapter adapter = new OracleDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dataTable;
        }

        public List<ProcessSegment> GetProcessSegments(string code)
        {
            List<ProcessSegment> segments = new List<ProcessSegment>();

            // 动态拼接SQL
            string query = @"SELECT * FROM TA_PROC t WHERE t.C_STEEL_NO != '*' AND t.C_PROC_CODE != 'CC4'";
            if (!string.IsNullOrWhiteSpace(code))
            {
                query += " AND t.C_STOVE_CC = :code";
            }
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            command.Parameters.Add(new OracleParameter("code", code));
                        }

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime startTime;
                                DateTime endTime;

                                // Parse date strings (adjust format as needed)
                                if (!DateTime.TryParse(reader["C_DTIME_STA"].ToString(), out startTime))
                                    continue;

                                if (!DateTime.TryParse(reader["C_DTIME_END"].ToString(), out endTime))
                                    continue;

                                // Map process codes to process names (customize based on your needs)
                                string processName = MapProcessCodeToName(reader["C_PROC_CODE"].ToString());

                                segments.Add(new ProcessSegment
                                {
                                    ProcessName = processName,
                                    StartTime = startTime.AddDays(43),
                                    EndTime = endTime.AddDays(43),
                                    Ty = reader["C_STOVE_CC"].ToString(),
                                    SteelNo = reader["C_STEEL_NO"].ToString(),
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading process segments: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return segments;
        }


        private string MapProcessCodeToName(string procCode)
        {
    
            switch (procCode)
            {
                case "LD1": return "1#转炉";
                case "LD2": return "2#转炉";
                case "LD3": return "3#转炉";
                case "LF1": return "1#LF精炼";
                case "LF2": return "2#LF精炼";
                case "LF3": return "3#LF精炼";
                case "LF4": return "4#LF精炼";
                case "LF5": return "5#LF精炼";
                case "LF6": return "6#LF精炼";
                case "LF7": return "7#LF精炼";
                case "LF8": return "8#LF精炼";
                case "RH1": return "1#RH精炼";
                case "RH2": return "2#RH精炼";
                case "RH3": return "3#RH精炼";
                case "RH4": return "4#RH精炼";
                case "VD1": return "1#VD精炼";
                case "VD2": return "2#VD精炼";
                case "CC1": return "1#连铸机";
                case "CC2": return "2#连铸机";
                case "CC3": return "3#连铸机";
                //case "CC4": return "4#连铸机";
                case "CC5": return "5#连铸机";
                case "CC6": return "6#连铸机";
                default: return procCode; 
            }
        }
    }
}