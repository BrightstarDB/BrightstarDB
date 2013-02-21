using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace BrightstarDB.SdShare
{
    public class EntityInfo {
        /// <summary>
        /// The id of the entity that will be used in the fragment feed
        /// </summary>
        public String EntityId;

        /// <summary>
        /// The key of the entity that is used for comparison. This is different from the EntityId when
        /// the key is a composite value but the id is not.
        /// </summary>
        public String EntityKey;

        /// <summary>
        /// The hashvalue for the entity. 
        /// </summary>
        public int HashValue;
    }

    public abstract class BaseDataSourceManager
    {
        public abstract IEnumerable<EntityInfo> GetEntityInfos();

        protected bool _inProgress;
        private string _hashValueFileName;

        public BaseDataSourceManager(string hashValueFileName)
        {
            _hashValueFileName = hashValueFileName;
        }

        public IEnumerable<UpdatedInfo> ListLastUpdated(DateTime since)
        {
            return LoadData().Values.Where(x => x.LastUpdated > since.ToUniversalTime()).Select(x => x);
        }

        protected Dictionary<string, UpdatedInfo> _data;

        private readonly object _lock = new object();

        protected Dictionary<string, UpdatedInfo> LoadData()
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


            UpdatedInfo info = null;
            var data = new Dictionary<string, UpdatedInfo>();
            try
            {
                //var fileNameExtention = _definition.HashValueFileName;
                //if (string.IsNullOrEmpty(fileNameExtention))
                //    fileNameExtention =  _definition.HashValueTable;

                var dataLocation = ConfigurationReader.Configuration.HashValueStorageLocation + Path.DirectorySeparatorChar + _hashValueFileName + ".slu";

                var fileInfo = new FileInfo(dataLocation);

                // if the file doesnt exist then 
                if (!fileInfo.Exists) return new Dictionary<string, UpdatedInfo>();

                using (var fs = new FileStream(dataLocation, FileMode.Open))
                {
                    using (var br = new BinaryReader(fs))
                    {
                        var count = br.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            info = new UpdatedInfo();
                            info.Id = br.ReadString();
                            info.EntityId = br.ReadString();
                            info.HashValue = br.ReadInt32();
                            info.LastUpdated = DateTime.Parse(br.ReadString());
                            data.Add(info.EntityId, info);
                        }
                    }
                    fs.Close();
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

        public void ProcessDataSource(object o)
        {
            lock(_lock) 
            {
                if (!_inProgress)
                {
                    Logging.LogInfo("Process Data Source Begun " + _hashValueFileName);
                    try
                    {
                        var entityLastUpdatedData = LoadData();
                        _inProgress = true;

                        var entityInfos = GetEntityInfos().ToList();
                        var entityKeys = new Dictionary<string, string>();

                        foreach (var entity in entityInfos)
	                    {
		                    // add to data for delete lookups later
                            try {
                                entityKeys.Add(entity.EntityKey, entity.EntityKey);
                            } catch (Exception ex){
                                Logging.LogError(1, "Unable to add duplicate id to entity keys");
                            }

                            UpdatedInfo info;
                            string keyValue = entity.EntityKey;
                            if (entityLastUpdatedData.TryGetValue(keyValue, out info))
                            {
                                if (info.HashValue != entity.HashValue)
                                {
                                    info.LastUpdated = DateTime.UtcNow;
                                    info.HashValue = entity.HashValue;
                                }
                            }
                            else
                            {
                                entityLastUpdatedData.Add(keyValue,
                                                        new UpdatedInfo()
                                                            {
                                                                Id = entity.EntityId,
                                                                EntityId = entity.EntityKey,
                                                                HashValue = entity.HashValue,
                                                                LastUpdated = DateTime.UtcNow
                                                            });
                            }
	                    }

                        foreach (var key in entityLastUpdatedData.Keys)
                        {
                            if (!entityKeys.ContainsKey(key))
                            {
                                entityLastUpdatedData[key].LastUpdated = DateTime.UtcNow;
                            }
                        }

                        // save index to disk
                        SaveData(entityLastUpdatedData);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(0,
                                         "Error Processing Data Source " + ex.Message +
                                         " " + ex.StackTrace);
                    }
                    finally
                    {
                        _inProgress = false;
                    }
                    Logging.LogInfo("Process Data Source Completed ");
                }
            }
        }

        private void SaveData(Dictionary<string, UpdatedInfo> data)
        {
            var dataLocation = ConfigurationReader.Configuration.HashValueStorageLocation + Path.DirectorySeparatorChar + _hashValueFileName + ".slu";

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
                fs.Close();
            }
            _data = null;
        }
    }
}
