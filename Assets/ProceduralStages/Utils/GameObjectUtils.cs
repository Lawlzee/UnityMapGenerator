using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    public static class GameObjectUtils
    {
        public static List<GameObject> FindMany(string path)
        {
            List<PathPart> pathParts = ParsePath(path);
            PathPart rootPart = pathParts[0];

            List<GameObject> currentGameObjects;

            if (rootPart.subPath != null)
            {
                currentGameObjects = new List<GameObject>();

                if (rootPart.quoted)
                {
                    GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

                    for (int i = 0; i < rootObjects.Length; i++)
                    {
                        GameObject child = rootObjects[i];
                        if (child.name == rootPart.subPath)
                        {
                            currentGameObjects.Add(child);
                            if (!rootPart.many)
                            {
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    GameObject gameObject = GameObject.Find(rootPart.subPath);

                    if (gameObject == null)
                    {
                        return currentGameObjects;
                    }

                    if (!rootPart.many)
                    {
                        currentGameObjects.Add(gameObject);
                    }
                    else
                    {
                        Transform parent = gameObject.transform.parent;
                        if (parent == null)
                        {
                            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

                            for (int i = 0; i < rootObjects.Length; i++)
                            {
                                GameObject child = rootObjects[i];
                                if (child.name == gameObject.name)
                                {
                                    currentGameObjects.Add(child);
                                }
                            }
                        }
                        else
                        {
                            int siblingCount = parent.childCount;

                            for (int i = 0; i < siblingCount; i++)
                            {
                                Transform child = parent.GetChild(i);
                                if (child.name == gameObject.name)
                                {
                                    currentGameObjects.Add(child.gameObject);
                                }
                            }
                        }
                    }
                }
            }
            else if (rootPart.many)
            {
                currentGameObjects = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            }
            else
            {
                throw new Exception($"Invalid path {path}"); 
            }

            for (int i = 1; i < pathParts.Count; i++)
            {
                PathPart pathPart = pathParts[i];
                List<GameObject> childObjects = new List<GameObject>();

                if (pathPart.subPath != null)
                {
                    foreach (GameObject parent in currentGameObjects)
                    {
                        if (pathPart.quoted)
                        {
                            Log.Debug(path + ": " + pathPart.subPath);
                            for (int j = 0; j < parent.transform.childCount; j++)
                            {
                                Transform child = parent.transform.GetChild(j);
                                Log.Debug(path + ": " + child.name);
                                if (child.name == pathPart.subPath)
                                {
                                    childObjects.Add(child.gameObject);
                                    if (!pathPart.many)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            GameObject gameObject = parent.transform.Find(pathPart.subPath)?.gameObject;
                            if (gameObject != null)
                            {
                                if (!pathPart.many)
                                {
                                    childObjects.Add(gameObject);
                                }
                                else
                                {
                                    Transform realParent = gameObject.transform.parent;
                                    int siblingCount = realParent.childCount;

                                    for (int j = 0; j < siblingCount; j++)
                                    {
                                        Transform child = realParent.GetChild(j);
                                        if (child.name == gameObject.name)
                                        {
                                            childObjects.Add(child.gameObject);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (pathPart.many)
                {
                    foreach (GameObject parent in currentGameObjects)
                    {
                        for (int j = 0; j < parent.transform.childCount; j++)
                        {
                            childObjects.Add(parent.transform.GetChild(j).gameObject);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Invalid path {path}");
                }

                currentGameObjects = childObjects;
            }

            return currentGameObjects;
        }

        private struct PathPart
        {
            public string subPath;
            public bool many;
            public bool quoted;
        }

        private static List<PathPart> ParsePath(string path)
        {
            var parts = new List<PathPart>();
            bool inQuote = false;
            bool isQuoted = false;
            bool isEscape = false;
            bool isMany = false;

            StringBuilder currentSubPath = new StringBuilder();
            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];

                if (!isEscape)
                {
                    if (c == '\\')
                    {
                        isEscape = true;
                    }
                    else if (c == '/')
                    {
                        if (!inQuote && (isQuoted || isMany))
                        {
                            parts.Add(new PathPart
                            {
                                quoted = isQuoted,
                                many = isMany,
                                subPath = currentSubPath.Length > 0
                                ? currentSubPath.ToString()
                                : null
                            });

                            currentSubPath.Clear();

                            isMany = false;
                            isQuoted = false;
                        }
                        else
                        {
                            currentSubPath.Append("/");
                        }
                        
                    }
                    else if (c == '"' || c == '*')
                    {
                        if (currentSubPath.ToString().EndsWith("/") && !inQuote)
                        {
                            currentSubPath.Remove(currentSubPath.Length - 1, 1);

                            parts.Add(new PathPart
                            {
                                quoted = false,
                                many = isMany,
                                subPath = currentSubPath.Length > 0
                                    ? currentSubPath.ToString()
                                    : null
                            });

                            currentSubPath.Clear();
                            isMany = false;
                        }

                        if (c == '"')
                        {
                            if (currentSubPath.Length == 0)
                            {
                                inQuote = true;
                            }
                            else if (i == path.Length - 1 || path[i + 1] == '/')
                            {
                                inQuote = false;
                                isQuoted = true;
                            }
                        }
                        else if (c == '*')
                        {
                            if (currentSubPath.Length == 0)
                            {
                                isMany = true;
                            }
                        }
                    }
                    else
                    {
                        isEscape = false;
                        currentSubPath.Append(c);
                    }
                }
                else
                {
                    currentSubPath.Append(c);
                }
            }

            parts.Add(new PathPart
            {
                quoted = isQuoted,
                many = isMany,
                subPath = currentSubPath.Length > 0
                    ? currentSubPath.ToString()
                    : null
            });

            string debugPath = string.Join(
                ", ",
                parts
                    .Select(x => $"{(x.quoted ? "[quoted] " : "")}{(x.many ? "[*] " : "")}{x.subPath ?? "<null>"}"));

            Log.Debug($"{path}= {debugPath}");

            return parts;
        }
    }
}
