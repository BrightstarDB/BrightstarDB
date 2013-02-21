using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;

namespace BrightstarNotes.DataModel
{
    public class MainViewModel 
    {
        private NotesContext _context;
        public ObservableCollection<INote> AllNotes { get; private set; }
        public ObservableCollection<INoteCategory> Categories { get; private set; }
        public bool IsDataLoaded { get; private set; }

        public MainViewModel()
        {
            _context = new NotesContext("type=embedded;storesDirectory=brightstar;storeName=brightstarNotes");
            AllNotes = new ObservableCollection<INote>();
            Categories = new ObservableCollection<INoteCategory>();
        }


        public NotesContext Context
        {
            get
            {
                return _context;
            }
        }

        public void LoadData()
        {
            AllNotes = new ObservableCollection<INote>(_context.Notes);
            Categories = new ObservableCollection<INoteCategory>(_context.NoteCategories);
            IsDataLoaded = true;
        }


        internal INoteCategory GetCategory(string selectedCategoryId)
        {
            return _context.NoteCategories.FirstOrDefault(x => x.Id.Equals(selectedCategoryId));
        }

        internal INote GetNote(string noteId)
        {
            return _context.Notes.FirstOrDefault(x => x.Id.Equals(noteId));
        }

        public void Refresh()
        {
            AllNotes.Clear();
            foreach (var n in _context.Notes) AllNotes.Add(n);
            Categories.Clear();
            foreach (var c in _context.NoteCategories) Categories.Add(c);
        }

        internal void DeleteObject(object o)
        {
            _context.DeleteObject(o);
            _context.SaveChanges();
        }
    }
}
