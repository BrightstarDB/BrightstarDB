using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Threading;

namespace BrightstarDB.SdShare
{
    public class DataSourceManager
    {
        private bool _inProgress;
        private readonly OdbcCollectionProvider _provider;
        private readonly ResourcePublishingDefinition _definition;

        public  DataSourceManager(OdbcCollectionProvider provider, ResourcePublishingDefinition definition)
        {
            _provider = provider;
            _definition = definition;
        }

        public IEnumerable<UpdatedInfo> ListLastUpdated(DateTime since)
        {
            return LoadData().Values.Where(x => x.LastUpdated > since.ToUniversalTime()).Select(x => x);
        }

        private Dictionary<string, UpdatedInfo> _data;

        private Dictionary<string, UpdatedInfo> LoadData()
        {
            // if we have the data available then return it
            if (_data != null)
            {
                return _data; 
            }

            // if we are processing the feed then the client needs to wait.
            while (_inProgress)
            {
                Thread.Sleep(50);
            }

            try
            {
                var fileNameExtention = _definition.HashValueFileName;
                if (string.IsNullOrEmpty(fileNameExtention))
                    fileNameExtention =  _definition.HashValueTable;

                var dataLocation = ConfigurationReader.Configuration.HashValueStorageLocation + Path.DirectorySeparatorChar + _provider.Name + "-" + fileNameExtention + ".slu";

                var fileInfo = new FileInfo(dataLocation);

                // if the file doesnt exist then 
                if (!fileInfo.Exists) return new Dictionary<string, UpdatedInfo>();

                var data = new Dictionary<string, UpdatedInfo>();
                using (var fs = new FileStream(dataLocation, FileMode.Open))
                {
                    using (var br = new BinaryReader(fs))
                    {
                        var count = br.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            var info = new UpdatedInfo();
                            info.Id = br.ReadString();
                            info.EntityId = br.ReadString();
                            info.HashValue = br.ReadInt32();
                            info.LastUpdated = DateTime.Parse(br.ReadString());
                            data.Add(info.Id, info);
                        }
                    }
                }
                _data = data;
                return _data;
            }
            catch (Exception ex)
            {
                Logging.LogError(1, "Error Loading Data {0} {1}", ex.Message, ex.StackTrace);
            }

            _data = new Dictionary<string, UpdatedInfo>();
            return _data;
        }

        private static readonly object _lock = new object();

        public void ProcessDataSource(object o)
        {
            lock(_lock) 
            {
                if (!_inProgress)
                {
                    Logging.LogInfo("Process Data Source Begun " + _definition.HashValueTable);
                    try
                    {
                        var entityUpdatedData = LoadData();
                        _inProgress = true;
                        using (var connection = new OdbcConnection(_provider.DsConnection))
                        {
                            connection.Open();
                            var odbcCommand = new OdbcCommand("select distinct * from " + _definition.HashValueTable)
                                                  {Connection = connection};
                            var dr = odbcCommand.ExecuteReader();
                            var data = new Dictionary<string, string>();
                            while (dr.Read())
                            {
                                var entityId = dr[_definition.EntityIdColumn].ToString().Trim();

                                // make string value
                                string hashInput = "";
                                for (int i = 0; i < dr.FieldCount; i++)
                                {
                                    hashInput += dr[i] + ";;axl;;";
                                }

                                var hashValue = hashInput.GetHashCode();
                                var keyValue = "";
                                var keyColumns = "";
                                foreach (var keyColumnName in _definition.HashValueKeyColumns)
                                {
                                    keyColumns += keyColumnName + ":";
                                    keyValue += dr[keyColumnName].ToString() + "::::";
                                }

                                try
                                {
                                    data.Add(keyValue, keyValue);
                                }
                                catch (Exception)
                                {
                                    Logging.LogError(1,
                                                     "Error adding key value " + _definition.HashValueTable + " " +
                                                     keyColumns + " " + keyValue);
                                }

                                UpdatedInfo info;
                                if (entityUpdatedData.TryGetValue(keyValue, out info))
                                {
                                    if (info.HashValue != hashValue)
                                    {
                                        info.LastUpdated = DateTime.UtcNow;
                                        info.HashValue = hashValue;
                                    }
                                }
                                else
                                {
                                    entityUpdatedData.Add(keyValue,
                                                          new UpdatedInfo()
                                                              {
                                                                  Id = keyValue,
                                                                  EntityId = entityId,
                                                                  HashValue = hashValue,
                                                                  LastUpdated = DateTime.UtcNow
                                                              });
                                }
                            }

                            dr.Close();

                            foreach (var key in entityUpdatedData.Keys)
                            {
                                if (!data.ContainsKey(key))
                                {
                                    entityUpdatedData[key].LastUpdated = DateTime.UtcNow;
                                }
                            }

                            // save index to disk
                            SaveData(entityUpdatedData);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(0,
                                         "Error Processing Data Source " + _definition.HashValueTable + " " + ex.Message +
                                         " " + ex.StackTrace);
                    }
                    finally
                    {
                        _inProgress = false;
                    }
                    Logging.LogInfo("Process Data Source Completed " + _definition.HashValueTable);
                }
            }
        }

        private void SaveData(Dictionary<string, UpdatedInfo> data)
        {
            // Axel was here
            var fileNameExtention = _definition.HashValueFileName;
            if (string.IsNullOrEmpty(fileNameExtention))
                fileNameExtention = _definition.HashValueTable;
            var dataLocation = ConfigurationReader.Configuration.HashValueStorageLocation + Path.DirectorySeparatorChar + _provider.Name + "-" + fileNameExtention + ".slu";

            using (var fs = new FileStream(dataLocation, FileMode.Create))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(data.Values.Count);
                    foreach (var updatedInfo in data.Values)
                    {
                        bw.Write(updatedInfo.Id);
                        bw.Write(updatedInfo.EntityId);
                        bw.Write(updatedInfo.HashValue);
                        bw.Write(updatedInfo.LastUpdated.ToString("s"));
                    }                    
                }                
            }
            _data = null;
        }
    }
}