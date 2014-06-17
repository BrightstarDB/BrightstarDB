﻿ 

// -----------------------------------------------------------------------
// <autogenerated>
//    This code was generated from a template.
//
//    Changes to this file may cause incorrect behaviour and will be lost
//    if the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework;

using System.Text;
using System.Net;

namespace GettingStarted.DataModel 
{
    public partial class NotesContext : BrightstarEntityContext {
    	
    	static NotesContext() 
    	{
    		var provider = new ReflectionMappingProvider();
    		provider.AddMappingsForType(EntityMappingStore.Instance, typeof(GettingStarted.DataModel.ICategory));
    		EntityMappingStore.Instance.SetImplMapping<GettingStarted.DataModel.ICategory, GettingStarted.DataModel.Category>();
    		provider.AddMappingsForType(EntityMappingStore.Instance, typeof(GettingStarted.DataModel.INote));
    		EntityMappingStore.Instance.SetImplMapping<GettingStarted.DataModel.INote, GettingStarted.DataModel.Note>();
    	}
    	
    	/// <summary>
    	/// Initialize a new entity context using the specified BrightstarDB
    	/// Data Object Store connection
    	/// </summary>
    	/// <param name="dataObjectStore">The connection to the BrightstarDB Data Object Store that will provide the entity objects</param>
    	public NotesContext(IDataObjectStore dataObjectStore) : base(dataObjectStore)
    	{
    		InitializeContext();
    	}
    
    	/// <summary>
    	/// Initialize a new entity context using the specified Brightstar connection string
    	/// </summary>
    	/// <param name="connectionString">The connection to be used to connect to an existing BrightstarDB store</param>
    	/// <param name="enableOptimisticLocking">OPTIONAL: If set to true optmistic locking will be applied to all entity updates</param>
        /// <param name="updateGraphUri">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="datasetGraphUris">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve entities and their properties.
        /// If not defined, all graphs in the store will be queried.</param>
        /// <param name="versionGraphUri">OPTIONAL: The URI identifier of the graph that contains version number statements for entities. 
        /// If not defined, the <paramref name="updateGraphUri"/> will be used.</param>
    	public NotesContext(
    	    string connectionString, 
    		bool? enableOptimisticLocking=null,
    		string updateGraphUri = null,
    		IEnumerable<string> datasetGraphUris = null,
    		string versionGraphUri = null
        ) : base(connectionString, enableOptimisticLocking, updateGraphUri, datasetGraphUris, versionGraphUri)
    	{
    		InitializeContext();
    	}
    
    	/// <summary>
    	/// Initialize a new entity context using the specified Brightstar
    	/// connection string retrieved from the configuration.
    	/// </summary>
    	public NotesContext() : base()
    	{
    		InitializeContext();
    	}
    	
    	/// <summary>
    	/// Initialize a new entity context using the specified Brightstar
    	/// connection string retrieved from the configuration and the
    	//  specified target graphs
    	/// </summary>
        /// <param name="updateGraphUri">The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// set to null, the default graph in the store will be updated.</param>
        /// <param name="datasetGraphUris">The URI identifiers of the graphs that will be queried to retrieve entities and their properties.
        /// If set to null, all graphs in the store will be queried.</param>
        /// <param name="versionGraphUri">The URI identifier of the graph that contains version number statements for entities. 
        /// If set to null, the value of <paramref name="updateGraphUri"/> will be used.</param>
    	public NotesContext(
    		string updateGraphUri,
    		IEnumerable<string> datasetGraphUris,
    		string versionGraphUri
    	) : base(updateGraphUri:updateGraphUri, datasetGraphUris:datasetGraphUris, versionGraphUri:versionGraphUri)
    	{
    		InitializeContext();
    	}
    	
    	private void InitializeContext() 
    	{
    		Categories = 	new BrightstarEntitySet<GettingStarted.DataModel.ICategory>(this);
    		Notes = 	new BrightstarEntitySet<GettingStarted.DataModel.INote>(this);
    	}
    	
    	public IEntitySet<GettingStarted.DataModel.ICategory> Categories
    	{
    		get; private set;
    	}
    	
    	public IEntitySet<GettingStarted.DataModel.INote> Notes
    	{
    		get; private set;
    	}
    	
    }
}
namespace GettingStarted.DataModel 
{
    
    public partial class Category : BrightstarEntityObject, ICategory 
    {
    	public Category(BrightstarEntityContext context, BrightstarDB.Client.IDataObject dataObject) : base(context, dataObject) { }
    	public Category() : base() { }
    	public System.String CategoryId { get {return GetKey(); } set { SetKey(value); } }
    	#region Implementation of GettingStarted.DataModel.ICategory
    
    	public System.String Label
    	{
            		get { return GetRelatedProperty<System.String>("Label"); }
            		set { SetRelatedProperty("Label", value); }
    	}
    	#endregion
    }
}
namespace GettingStarted.DataModel 
{
    
    public partial class Note : BrightstarEntityObject, INote 
    {
    	public Note(BrightstarEntityContext context, BrightstarDB.Client.IDataObject dataObject) : base(context, dataObject) { }
    	public Note() : base() { }
    	public System.String NoteId { get {return GetKey(); } set { SetKey(value); } }
    	#region Implementation of GettingStarted.DataModel.INote
    
    	public System.String Label
    	{
            		get { return GetRelatedProperty<System.String>("Label"); }
            		set { SetRelatedProperty("Label", value); }
    	}
    
    	public System.String Content
    	{
            		get { return GetRelatedProperty<System.String>("Content"); }
            		set { SetRelatedProperty("Content", value); }
    	}
    
    	public GettingStarted.DataModel.ICategory Category
    	{
            get { return GetRelatedObject<GettingStarted.DataModel.ICategory>("Category"); }
            set { SetRelatedObject<GettingStarted.DataModel.ICategory>("Category", value); }
    	}
    	#endregion
    }
}
