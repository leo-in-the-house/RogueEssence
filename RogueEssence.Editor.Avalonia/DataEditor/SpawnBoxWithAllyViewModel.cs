using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;
using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using Avalonia.Controls;
using RogueElements;
using RogueEssence.Dev.Views;

namespace RogueEssence.Dev.ViewModels
{
    public class SpawnBoxWithAllyElement : ViewModelBase
    {
        private bool isAlly;
        public bool IsAlly
        {
            get { return isAlly; }
            set { this.SetIfChanged(ref isAlly, value); }
        }
        private object val;
        public object Value
        {
            get { return val; }
        }
        public string DisplayValue
        {
            get { return conv.GetString(val); }
        }

        private StringConv conv;
        
        public SpawnBoxWithAllyElement(StringConv conv, bool isAlly, object val)
        {
            this.conv = conv;
            this.isAlly = isAlly;
            this.val = val;
        }
    }

    public class SpawnBoxWithAllyViewModel : ViewModelBase
    {
        public delegate void EditElementOp(int index, object element);
        public delegate void ElementOp(int index, object element, EditElementOp op);

        public event ElementOp OnEditItem;

        public StringConv StringConv;

        private Window parent;

        public bool ConfirmDelete;

        public SpawnBoxWithAllyViewModel(Window parent, StringConv conv)
        {
            StringConv = conv;
            this.parent = parent;
            Collection = new ObservableCollection<SpawnBoxWithAllyElement>();
        }

        public ObservableCollection<SpawnBoxWithAllyElement> Collection { get; }

        private int currentElement;
        public int CurrentElement
        {
            get { return currentElement; }
            set
            {
                this.SetIfChanged(ref currentElement, value);
                if (currentElement > -1)
                    CurrentIsAlly = Collection[currentElement].IsAlly;
                else
                    CurrentIsAlly = false;
            }
        }
        
        private bool currentIsAlly;
        public bool CurrentIsAlly
        {
            get { return currentIsAlly; }
            set
            {
                this.SetIfChanged(ref currentIsAlly, value);
                if (currentElement > -1)
                {
                    Collection[currentElement].IsAlly = currentIsAlly;
                }
            }
        }
        
        private void editItem(int index, object element)
        {
            index = Math.Min(Math.Max(0, index), Collection.Count);
            Collection[index] = new SpawnBoxWithAllyElement(StringConv, Collection[index].IsAlly, element);
            CurrentElement = index;
        }

        private void insertItem(int index, object element)
        {
            index = Math.Min(Math.Max(0, index), Collection.Count + 1);
            Collection.Insert(index, new SpawnBoxWithAllyElement(StringConv, false, element));
            CurrentElement = index;
        }

        public void InsertOnKey(int index, object element)
        {
            bool newIsAlly = false;
            if (0 <= index && index < Collection.Count)
            {
                newIsAlly = Collection[index].IsAlly;
            }

            index = Math.Min(Math.Max(0, index), Collection.Count + 1);
            Collection.Insert(index, new SpawnBoxWithAllyElement(StringConv, newIsAlly, element));
            CurrentElement = index;
        }

        public void gridCollection_DoubleClick(object sender, RoutedEventArgs e)
        {
            //int index = lbxCollection.IndexFromPoint(e.X, e.Y);
            int index = CurrentElement;
            if (index > -1)
            {
                SpawnBoxWithAllyElement element = Collection[index];
                OnEditItem?.Invoke(index, element.Value, editItem);
            }
        }


        private void btnAdd_Click()
        {
            int index = CurrentElement;
            if (index < 0)
                index = Collection.Count;
            object element = null;
            OnEditItem?.Invoke(index, element, insertItem);
        }

        private async void btnDelete_Click()
        {
            if (CurrentElement > -1 && CurrentElement < Collection.Count)
            {
                if (ConfirmDelete)
                {
                    MessageBox.MessageBoxResult result = await MessageBox.Show(parent, "Are you sure you want to delete this item:\n" + Collection[currentElement].DisplayValue, "Confirm Delete",
                    MessageBox.MessageBoxButtons.YesNo);
                    if (result == MessageBox.MessageBoxResult.No)
                        return;
                }

                Collection.RemoveAt(CurrentElement);
            }
        }

        private void Switch(int a, int b)
        {
            SpawnBoxWithAllyElement obj = Collection[a];
            Collection[a] = Collection[b];
            Collection[b] = obj;
        }

        private void btnUp_Click()
        {
            if (CurrentElement > 0)
            {
                int index = CurrentElement;
                Switch(CurrentElement, CurrentElement - 1);
                CurrentElement = index - 1;
            }
        }

        private void btnDown_Click()
        {
            if (CurrentElement > -1 && CurrentElement < Collection.Count - 1)
            {
                int index = CurrentElement;
                Switch(CurrentElement, CurrentElement + 1);
                CurrentElement = index + 1;
            }
        }

    }
}
