namespace UnityMergeTool
{
    public class DiffableProperty<T>
    {
        public T      value;
        public T      oldValue;
        public bool   valueChanged;
        public bool   assigned = false; // Later can use this to denote what was actually read from the yaml
    }
}