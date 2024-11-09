using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace Editor.Custom
{
    public class RoundedRectCreator : EditorWindow
    {
        private const string PathSaveKey = "path_round_rect";
        private const float ZOffset = -90000;

        private float radius;
        private float width;
        private float height;
        private string saveDirectory;
        private Color customColor = Color.white;
        private float widthLine;

        private readonly List<Mesh> previewMeshes = new();
        private bool createdMeshForPreview;
        private Material materialPreview;
        private float rectSizePreview;
        private bool leftTopCorner = true;
        private bool leftBottomCorner = true;
        private bool rightTopCorner = true;
        private bool rightBottomCorner = true;
        private Scene tempScene;

        [MenuItem("Window/Round rect creator")]
        public static void Display()
        {
            var window = GetWindow<RoundedRectCreator>();
            window.titleContent = new GUIContent("Round rect creator");

            window.Show();
        }

        private void OnHierarchyChange()
        {
            EditorCustomUtility.ClearTextures();
        }

        private void OnEnable()
        {
            string defValPathSave = Path.Combine("Assets", "Sprites");

            EditorCustomUtility.ClearTextures();
            saveDirectory = EditorPrefs.GetString(PathSaveKey, defValPathSave);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("BOX");
            
            EditorGUI.BeginChangeCheck();

            radius = EditorGUILayout.FloatField(new GUIContent("Radius"), radius);
            widthLine = EditorGUILayout.FloatField(new GUIContent("Width Line"), widthLine);
            
            GUI.enabled = radius > 0;
            rectSizePreview = EditorGUILayout.Slider(new GUIContent("Preview rect size"),rectSizePreview,0,500);
            GUI.enabled = true;
            
            /*width = EditorGUILayout.FloatField(new GUIContent("Custom width"), width);
            height = EditorGUILayout.FloatField(new GUIContent("Custom height"), height);*/
            customColor = EditorGUILayout.ColorField(new GUIContent("Custom color"), customColor);

            CornerSelector();

            if (EditorGUI.EndChangeCheck())
            {
                if (radius <= 0)
                {
                    rectSizePreview = 0;
                }
                PrepareForPreview();
            }

            EditorGUI.BeginChangeCheck();
            saveDirectory = EditorGUILayout.TextField(new GUIContent("Save directory"), saveDirectory);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PathSaveKey, saveDirectory);
            }



            GUI.enabled = radius > 0 && saveDirectory != string.Empty;
            if (GUILayout.Button("Create"))
            {
                CreateRectImage(false);
                EditorCustomUtility.ClearTextures();
            }

            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            DrawPreview();
        }

        private void CornerSelector()
        {
            var controlRect = EditorGUILayout.GetControlRect(false,100);

            var centerRect = new Rect(0,0, 
                controlRect.width / 1.4f, controlRect.height / 1.4f);

            centerRect.position = new Vector2(controlRect.x + (controlRect.width / 2 - centerRect.width / 2),
                controlRect.y + (controlRect.height / 2- centerRect.height/2));
            
            EditorCustomUtility.DrawRect(controlRect,new Color(0.54f, 0.5f, 0.54f));
            EditorCustomUtility.DrawRect(centerRect,new Color(0.38f, 0.5f, 0.77f));

            var sizeToggle = new Vector2(14,14);
            leftTopCorner = EditorGUI.Toggle(new Rect(new Vector2(centerRect.position.x-sizeToggle.x,centerRect.position.y-sizeToggle.y),
                sizeToggle), leftTopCorner);
            rightTopCorner = EditorGUI.Toggle(new Rect(new Vector2(centerRect.position.x+centerRect.width,centerRect.position.y-sizeToggle.y),
                sizeToggle), rightTopCorner);
            rightBottomCorner = EditorGUI.Toggle(new Rect(new Vector2(centerRect.position.x+centerRect.width,centerRect.position.y+centerRect.height),
                sizeToggle), rightBottomCorner);
            leftBottomCorner = EditorGUI.Toggle(new Rect(new Vector2(centerRect.position.x-sizeToggle.x,centerRect.position.y+centerRect.height),
                sizeToggle), leftBottomCorner);

            var labelCorners = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30
            };
            GUI.Label(centerRect,"Corners",labelCorners);
        }

        private void PrepareForPreview()
        {
            previewMeshes.Clear();
            if(radius > 0 && radius > widthLine)
                CreateRectImage(true);
        }

        private void DrawPreview()
        {
            var controlRect = EditorGUILayout.GetControlRect();
            var rectPreview = new Rect(controlRect.position, new Vector2(controlRect.size.x, controlRect.size.x));
            EditorCustomUtility.DrawRect(rectPreview, new Color(0.54f, 0.64f, 0.63f));


            if (Event.current.type == EventType.Repaint)
            {
                if(materialPreview == null)
                    materialPreview = new Material(Shader.Find("Unlit/Color"));
                
                materialPreview.color = customColor;
                materialPreview.SetPass(0);
                var scale =  rectPreview.width / GetWidthFromRadius();
                foreach (var previewMesh in previewMeshes)
                {
                    if (previewMesh != null)
                    {
                        
                        Graphics.DrawMeshNow(previewMesh,
                            Matrix4x4.TRS(rectPreview.position +new Vector2(rectPreview.width/2,rectPreview.height/2),
                                Quaternion.Euler(0,180,0),
                                new Vector3(scale, scale, 1)));
                    }
                }
            }
        }

        

        private void CreateRectImage(bool preview)
        {
            Camera camera = null;

            createdMeshForPreview = preview;
            if (!preview)
            {
                StartEmptyScene();
                
                camera = CreateCamera();
            }

            float w = GetWidthFromRadius();


            if (widthLine > 0)
            {
                RectLineRound(-w / 2, -w / 2, w, w, radius, widthLine);
            }
            else
            {
                RectRound(-w / 2, -w / 2, w, w, radius);
            }

            if(preview) return;

            RenderTexture.active = camera.targetTexture;

            var texture2D = new Texture2D(camera.targetTexture.width, camera.targetTexture.height,
                GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);


            camera.Render();
            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);

            texture2D.Apply();

            RenderTexture.active = null;

            var pngBytes = texture2D.EncodeToPNG();

            string saveImagePath = "";

            if (widthLine > 0)
            {
                saveImagePath = Path.Combine(saveDirectory, $"round_{radius}_line_{widthLine}.png");
            }
            else
            {
                saveImagePath = Path.Combine(saveDirectory, $"round_{radius}.png");
            }

            File.WriteAllBytes(saveImagePath, pngBytes);
            EditorSceneManager.CloseScene(tempScene,true);
            AssetDatabase.Refresh();

            SetBorderToSprite(saveImagePath);
        }

        private float GetWidthFromRadius()
        {
            if (createdMeshForPreview)
            {
                return radius * 2 + 4 + rectSizePreview;
            }
            return radius * 2 + 4;
        }

        private void SetBorderToSprite(string saveImagePath)
        {
            float borderNew = radius + 1;
            var importer = AssetImporter.GetAtPath(saveImagePath) as TextureImporter;
            importer.spriteBorder = new Vector4(borderNew, borderNew, borderNew, borderNew);
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        private Mesh CreateMesh()
        {
            if (createdMeshForPreview)
            {
                var previewMesh = new Mesh();
                
                previewMeshes.Add(previewMesh);
                return previewMesh;
            }
            
            var meshGo = new GameObject("mesh")
            {
                layer = LayerMask.NameToLayer("UI")
            };

            var meshRenderer = meshGo.AddComponent<MeshRenderer>();

            var material = new Material(Shader.Find("Unlit/Color"))
            {
                color = customColor
            };

            meshGo.transform.position = new Vector3(0, 0, ZOffset);
            meshRenderer.material = material;
            var meshFilter = meshGo.AddComponent<MeshFilter>();

            var mesh = new Mesh();
            meshFilter.mesh = mesh;

            return mesh;
        }

        private Camera CreateCamera()
        {
            var cameraGO = new GameObject("camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.cullingMask = LayerMask.GetMask("UI");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1, 1, 1, 0);
            camera.orthographic = true;

            int size = (int) GetWidthFromRadius();

            var renderTexture = new RenderTexture(size, size, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                antiAliasing = 8
            };
            camera.targetTexture = renderTexture;

            camera.orthographicSize = size / 2 + 0.5f;
            camera.rect = new Rect(0, 0, size, size);


            cameraGO.transform.position = new Vector3(0, 0, ZOffset-1);
            return camera;
        }

        /// <summary>
        /// Открываем новую пустую сцену
        /// </summary>
        private void StartEmptyScene()
        {
            tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            tempScene.name = "Temp scene";
        }

        private float CubeRoot(float d)
        {
            return Mathf.Pow(Mathf.Abs(d), 1f / 3f) * Mathf.Sign(d);
        }

        private void RectRound(float x, float y, float width, float height, float radius)
        {
            //center
            Rect(x + radius, y + radius, width - radius * 2, height - radius * 2);
            //bottom
            Rect(x + radius, y, width - radius * 2, radius);
            //left
            Rect(x, y + radius, radius, height - radius * 2);
            //right
            Rect(x + (width - radius), y + radius, radius, height - radius * 2);
            //top
            Rect(x + radius, y + (height - radius), width - radius * 2, radius);


            if (createdMeshForPreview)
            {
                // left bottom corner
                if (rightTopCorner)
                {
                    Arc(x + radius, y + radius, radius, 180, 90);
                }
                else
                {
                    Rect(x , y,radius,radius);
                }
                //left top corner
                if (rightBottomCorner)
                {
                    Arc(x + radius, y + (height - radius), radius, 90, 90);
                }
                else
                {
                    Rect(x, y + (height - radius) ,radius,radius);
                }
                // right top corner
                if (leftBottomCorner)
                {
                    Arc(x + (width - radius), y + (height - radius), radius, 0, 90);
                }
                else
                {
                    Rect(x + (width - radius), y + (height - radius),radius,radius);
                }
                // right bottom corner
                if (leftTopCorner)
                {
                    Arc(x + (width - radius), y + radius, radius, 270, 90);
                }
                else
                {
                    Rect(x +(width-radius), y ,radius,radius);
                }
            }
            else
            {
                // left bottom corner
                if (leftBottomCorner)
                {
                    Arc(x + radius, y + radius, radius, 180, 90);
                }
                else
                {
                    Rect(x , y,radius,radius);
                }
                //left top corner
                if (leftTopCorner)
                {
                    Arc(x + radius, y + (height - radius), radius, 90, 90);
                }
                else
                {
                    Rect(x, y + (height - radius) ,radius,radius);
                }
                // right top corner
                if (rightTopCorner)
                {
                    Arc(x + (width - radius), y + (height - radius), radius, 0, 90);
                }
                else
                {
                    Rect(x + (width - radius), y + (height - radius),radius,radius);
                }
                // right bottom corner
                if (rightBottomCorner)
                {
                    Arc(x + (width - radius), y + radius, radius, 270, 90);
                }
                else
                {
                    Rect(x +(width-radius), y ,radius,radius);
                }
            }
            
        }

        private void RectLineRound(float x, float y, float width, float height, float radius, float widthLine)
        {
            //bottom
            Rect(x + radius, y, width - radius * 2, widthLine);
            //left
            Rect(x, y + radius, widthLine, height - radius * 2);
            //right
            Rect(x + width - widthLine, y + radius, widthLine, height - radius * 2);
            //top
            Rect(x + radius, y + height - widthLine, width - radius * 2, widthLine);


            if (createdMeshForPreview)
            {
                // left bottom corner
                if (rightTopCorner)
                {
                    ArcLine(x + radius, y + radius, radius, 180, 90, widthLine);
                }
                else
                {
                    Rect(x , y,radius,widthLine);
                    Rect(x , y,widthLine,radius);
                }
                //left top corner
                if (rightBottomCorner)
                {
                    ArcLine(x + radius, y + (height - radius), radius, 90, 90, widthLine);
                }
                else
                {
                    Rect(x , y+(height-widthLine),radius,widthLine);
                    Rect(x , y+(height-radius),widthLine,radius);
                }
                // right top corner
                if (leftBottomCorner)
                {
                    ArcLine(x + (width - radius), y + (height - radius), radius, 0, 90, widthLine);
                }
                else
                {
                    Rect(x+(width-radius) , y+(height-widthLine),radius,widthLine);
                    Rect(x +(width-widthLine), y+(height-radius),widthLine,radius); 
                }
                // right bottom corner
                if (leftTopCorner)
                {
                    ArcLine(x + (width - radius), y + radius, radius, 270, 90, widthLine);
                }
                else
                {
                    Rect(x+(width-radius) , y,radius,widthLine);
                    Rect(x +(width-widthLine), y+widthLine,widthLine,radius); 
                }
            }
            else
            {
                // left bottom corner
                if (leftBottomCorner)
                {
                    ArcLine(x + radius, y + radius, radius, 180, 90, widthLine);
                }
                else
                {
                    Rect(x , y,radius,widthLine);
                    Rect(x , y,widthLine,radius);
                }
                //left top corner
                if (leftTopCorner)
                {
                    ArcLine(x + radius, y + (height - radius), radius, 90, 90, widthLine);
                }
                else
                {
                    Rect(x , y+(height-widthLine),radius,widthLine);
                    Rect(x , y+(height-radius),widthLine,radius);
                }
                // right top corner
                if (rightTopCorner)
                {
                    ArcLine(x + (width - radius), y + (height - radius), radius, 0, 90, widthLine);
                }
                else
                {
                    Rect(x+(width-radius) , y+(height-widthLine),radius,widthLine);
                    Rect(x +(width-widthLine), y+(height-radius),widthLine,radius); 
                }
                // right bottom corner
                if (rightBottomCorner)
                {
                    ArcLine(x + (width - radius), y + radius, radius, 270, 90, widthLine);
                }
                else
                {
                    Rect(x+(width-radius) , y,radius,widthLine);
                    Rect(x +(width-widthLine), y+widthLine,widthLine,radius); 
                }
            }
            
        }

        private void Rect(float x, float y, float width, float height)
        {
            var mesh = CreateMesh();
            List<int> triangles = new List<int>()
            {
                0, 1, 2,
                0, 2, 3
            };

            var vertex = new List<Vector3>();


            vertex.Add(new Vector3(x, y));
            vertex.Add(new Vector3(x, y + height));
            vertex.Add(new Vector3(x + width, y + height));
            vertex.Add(new Vector3(x + width, y));

            mesh.vertices = vertex.ToArray();
            mesh.triangles = triangles.ToArray();
        }

        private void Arc(float x, float y, float radius, float startDeg, float degrees)
        {
            var mesh = CreateMesh();
            var vertex = new List<Vector3>();
            var indices = new List<int>();


            int segments = Mathf.Max(1, (int) (120 * CubeRoot(radius))) * 3;

            float theta = (2 * Mathf.PI * (degrees / 360.0f)) / segments;
            float cos = Mathf.Cos(theta);
            float sin = Mathf.Sin(theta);
            float cx = (radius) * Mathf.Cos(startDeg * Mathf.Deg2Rad);
            float cy = (radius) * Mathf.Sin(startDeg * Mathf.Deg2Rad);


            int numInd = 1;
            vertex.Add(new Vector3(x, y));
            vertex.Add(new Vector3(x + cx, y + cy));

            indices.Add(0);
            indices.Add(1);

            for (int i = 0; i < segments; i++)
            {
                float temp = cx;
                cx = cos * cx - sin * cy;
                cy = sin * temp + cos * cy;


                vertex.Add(new Vector3(x + cx, y + cy));
                int lastNumInd = numInd;
                numInd++;
                if (i == 0)
                {
                    indices.Add(numInd);
                }

                indices.Add(0);
                indices.Add(numInd);
                indices.Add(lastNumInd);
            }

            var indicesCount = indices.Count;

            int divider = indicesCount % 3;
            if (divider != 0)
            {
                indicesCount--;
                for (int i = 0; i < divider; i++)
                {
                    indices.RemoveAt(indicesCount--);
                }
            }

            mesh.vertices = vertex.ToArray();
            mesh.triangles = indices.ToArray();
        }

        private void ArcLine(float x, float y, float radius, float start, float degrees, float widthLine)
        {
            var mesh = CreateMesh();
            var vertex = new List<Vector3>();
            var indices = new List<int>();

            int segments = Mathf.Max(1, (int) (120 * CubeRoot(radius))) * 3;


            float angle = (2 * Mathf.PI * (degrees / 360.0f)) / segments;

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            //p1
            float cx1 = (radius - widthLine) * Mathf.Cos(start * Mathf.Deg2Rad);
            float cy1 = (radius - widthLine) * Mathf.Sin(start * Mathf.Deg2Rad);

            //p2
            float cx2 = radius * Mathf.Cos(start * Mathf.Deg2Rad);
            float cy2 = radius * Mathf.Sin(start * Mathf.Deg2Rad);


            vertex.Add(new Vector3(x + cx1, y + cy1));
            vertex.Add(new Vector3(x + cx2, y + cy2));


            int startInd = 0;

            for (int i = 0; i < segments; i++)
            {
                float temp = cx1;
                cx1 = cos * cx1 - sin * cy1;
                cy1 = sin * temp + cos * cy1;


                temp = cx2;
                cx2 = cos * cx2 - sin * cy2;
                cy2 = sin * temp + cos * cy2;


                vertex.Add(new Vector3(x + cx1, y + cy1));
                vertex.Add(new Vector3(x + cx2, y + cy2));


                indices.Add(startInd + 3);
                indices.Add(startInd + 1);
                indices.Add(startInd + 2);

                indices.Add(startInd + 2);
                indices.Add(startInd + 1);
                indices.Add(startInd);

                startInd += 2;
            }


            mesh.vertices = vertex.ToArray();
            mesh.triangles = indices.ToArray();
        }
    }
}