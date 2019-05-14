using System.Linq;
using VDS.RDF;
using VDS.RDF.Update;
using VDS.RDF.Update.Commands;

namespace BrightstarDB.Update
{
    internal class BrightstarUpdateProcessor : GenericUpdateProcessor
    {
        private readonly BrightstarIOManager _manager;
        public BrightstarUpdateProcessor(BrightstarIOManager manager) : base(manager)
        {
            _manager = manager;
        }

        public override void ProcessCommand(SparqlUpdateCommand cmd)
        {
            if (cmd is ClearCommand)
            {
                ProcessClearCommand(cmd as ClearCommand);
            }
            else if (cmd is CopyCommand)
            {
                ProcessCopyCommand(cmd as CopyCommand);
            }
            else if (cmd is MoveCommand)
            {
                ProcessMoveCommand(cmd as MoveCommand);
            }
            else if (cmd is AddCommand)
            {
                ProcessAddCommand(cmd as AddCommand);
            }
            else
            {
                base.ProcessCommand(cmd);
            }
        }

        new public void ProcessAddCommand(AddCommand cmd)
        {
            try
            {
                //If Source and Destination are equal this is a no-op
                if (EqualityHelper.AreUrisEqual(cmd.SourceUri, cmd.DestinationUri)) return;
                if (cmd.SourceUri != null && !_manager.ListGraphs().Any(g => EqualityHelper.AreUrisEqual(cmd.SourceUri, g)))
                {
                    throw new SparqlUpdateException(string.Format("Could not find source graph <{0}>", cmd.SourceUri));
                }
                _manager.AddGraph(cmd.SourceUri, cmd.DestinationUri);
            }
            catch
            {
                if (!cmd.Silent) throw;
            }
        }

        new public void ProcessMoveCommand(MoveCommand cmd)
        {
            try
            {
                //If Source and Destination are equal this is a no-op
                if (EqualityHelper.AreUrisEqual(cmd.SourceUri, cmd.DestinationUri)) return;
                if (cmd.SourceUri != null && !_manager.ListGraphs().Any(g => EqualityHelper.AreUrisEqual(cmd.SourceUri, g)))
                {
                    throw new SparqlUpdateException(string.Format("Could not find source graph <{0}>", cmd.SourceUri));
                }
                _manager.MoveGraph(cmd.SourceUri, cmd.DestinationUri);
            }
            catch
            {
                if (!cmd.Silent) throw;
            }
        }

        new public void ProcessCopyCommand(CopyCommand cmd)
        {
            try
            {
                //If Source and Destination are equal this is a no-op
                if (EqualityHelper.AreUrisEqual(cmd.SourceUri, cmd.DestinationUri)) return;
                if (cmd.SourceUri != null && !_manager.ListGraphs().Any(g=>EqualityHelper.AreUrisEqual(cmd.SourceUri, g)))
                {
                    throw new SparqlUpdateException(string.Format("Could not find source graph <{0}>", cmd.SourceUri));
                }
                _manager.CopyGraph(cmd.SourceUri, cmd.DestinationUri);
            }
            catch
            {
                if (!cmd.Silent) throw;
            }
        }

        new public void ProcessClearCommand(ClearCommand cmd)
        {
            switch (cmd.Mode)
            {
                case ClearMode.Default:
                case ClearMode.Graph:
                    _manager.DeleteGraph(cmd.TargetUri);
                    break;
                case ClearMode.Named:
                    _manager.DeleteGraphs(_manager.ListGraphs().Select(u=>u.ToString()));
                    break;
                case ClearMode.All:
                    _manager.DeleteGraphs(_manager.ListGraphs().Select(u=>u.ToString()).Union(new string[]{Constants.DefaultGraphUri}));
                    break;
            }
        }
    }
}