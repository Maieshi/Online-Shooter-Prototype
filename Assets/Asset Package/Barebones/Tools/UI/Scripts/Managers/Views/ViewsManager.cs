using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aevien.UI
{
    public static class ViewsManager
    {
        private static Dictionary<string, IUIView> views;

        static ViewsManager()
        {
            views = new Dictionary<string, IUIView>();
        }

        public static void Register(string viewId, IUIView view)
        {
            views[viewId] = view;
        }

        public static T GetView<T>(string viewId) where T : class, IUIView
        {
            if (views.TryGetValue(viewId, out IUIView view))
            {
                return (T)view;
            }
            else
            {
                Debug.LogError($"View with Id {viewId} is not registered");
                return null;
            }
        }

        public static void Show(string viewId)
        {
            if (views.TryGetValue(viewId, out IUIView view))
            {
                view.Show();
            }
            else
            {
                Debug.LogError($"View with Id {viewId} is not registered");
            }
        }

        public static void Hide(string viewId)
        {
            if (views.TryGetValue(viewId, out IUIView view))
            {
                view.Hide();
            }
            else
            {
                Debug.LogError($"View with Id {viewId} is not registered");
            }
        }

        public static void HideAllViews(bool instantly = false)
        {
            foreach (var view in views.Values)
            {
                view.Hide(instantly);
            }
        }

        public static void HideViewsByName(bool instantly = false, params string[] names)
        {
            if (names.Length == 0) return;

            foreach(string n in names)
            {
                if(views.TryGetValue(n, out IUIView view))
                {
                    view.Hide(instantly);
                }
            }
        }
    }
}