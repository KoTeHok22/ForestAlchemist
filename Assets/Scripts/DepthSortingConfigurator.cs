using UnityEngine;

/// <summary>
/// Централизованная система настройки depth-сортировки.
/// Применяется к любому объекту: декорации мира, орки, базы, статуи.
/// </summary>
public static class DepthSortingConfigurator
{
    /// <summary>
    /// Настраивает depth-сортировку на всех SpriteRenderer в иерархии.
    /// "Square" объекты получают sortingOrder = 0 (земля).
    /// Остальные получают SpriteDepthSorter для Y-сортировки.
    /// </summary>
    public static void ConfigureHierarchy(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
            {
                continue;
            }

            if (sr.gameObject.name == "Square")
            {
                SpriteDepthSorter existingSorter = sr.GetComponent<SpriteDepthSorter>();
                if (existingSorter != null)
                {
                    Object.Destroy(existingSorter);
                }

                sr.sortingOrder = 0;
                continue;
            }

            SpriteDepthSorter sorter = sr.GetComponent<SpriteDepthSorter>();
            if (sorter == null)
            {
                sorter = sr.gameObject.AddComponent<SpriteDepthSorter>();
            }

            sorter.RefreshSortingOrder();
        }
    }

    /// <summary>
    /// Настраивает depth-сортировку на одном объекте (без рекурсии).
    /// </summary>
    public static void ConfigureSingle(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            return;
        }

        SpriteDepthSorter sorter = obj.GetComponent<SpriteDepthSorter>();
        if (sorter == null)
        {
            sorter = obj.AddComponent<SpriteDepthSorter>();
        }

        sorter.RefreshSortingOrder();
    }
}
