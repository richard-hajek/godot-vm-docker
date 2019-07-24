using Godot;

namespace hackfest2.addons.Utils
{
    public static class Nodes
    {
        public static T FindChildOfType<T>(Node parent)
        {
            foreach (var child in parent.GetChildren())
                if (child is T t)
                    return t;

            return default(T);
        }
    }
}