using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unigine;

namespace UnigineApp.data.Code
{
    internal class Import_New
    {
        private readonly Dictionary<string, Mesh> _meshes = new();
        private readonly Dictionary<string, Material> _materials = new();
        private readonly HashSet<Guid> _guids = new();
        private Importer importer;

        string formattedPath;

        public Node import(string filepath)
        {
            // string relativePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "../data/ImportModel");
            // string fullPath = System.IO.Path.GetFullPath(relativePath);
            // formattedPath = filepath.Replace('\\', '/');

            string formattedPath = "D:\\UNIGINE\\dll_chaos\\models";

            importer = Import.CreateImporterByFileName(filepath);
            if (importer == null || !importer.Init(filepath))
            {
                return null;
            }

            ImportScene scene = importer.GetScene();

            for (int i = 0; i < scene.GetNumMeshes(); i++)
            {
                var importProcessor = new ImportProcessor();
                importProcessor.OutputPath = formattedPath + "/ImportModel";
               // scene.GetMesh(i).Name = scene.GetMesh(i).Name + CountModel.ToString();
                var mesh = new Mesh();

                importer.ImportMesh(importProcessor, mesh, scene.GetMesh(i));

                // UV(mesh);
                OnProcessMesh(mesh, scene.GetMesh(i));
                

                if (mesh.NumBones > 0)
                {
                    int numAnimations = scene.GetNumAnimations();
                    /*
                    for (int animIndex = 0; animIndex < numAnimations; animIndex++)
                    {
                        MeshAnimation animationMesh = new MeshAnimation();
                        importer.ImportAnimation(new ImportProcessor(), animationMesh,
                            scene.GetMesh(i), scene.GetAnimation(animIndex));

                        OnProcessAnimation(animationMesh, scene.GetMesh(i), scene.GetAnimation(animIndex));
                    }
                    */

                    MeshAnimation animationMesh = new MeshAnimation();
                    importer.ImportAnimation(importProcessor, animationMesh,
                        scene.GetMesh(i), scene.GetAnimation(0));

                    OnProcessAnimation(animationMesh, scene.GetMesh(i), scene.GetAnimation(0));
                }
            }

            for (int i = 0; i < scene.GetNumTextures(); i++)
            {
                var importProcessor = new ImportProcessor();
                importProcessor.OutputPath = formattedPath + "/ImportModel";

                importer.ImportTexture(importProcessor, scene.GetTexture(i));
            }

            var meshBase = Materials.FindManualMaterial("Unigine::mesh_base");

            for (int materialIndex = 0; materialIndex < scene.GetNumMaterials(); materialIndex++)
            {
                var importProcessor = new ImportProcessor();
                importProcessor.OutputPath = formattedPath + "/ImportModel";

                var material = meshBase.Inherit();
                importer.ImportMaterial(importProcessor, material, scene.GetMaterial(materialIndex));
                OnProcessMaterial(material, scene.GetMaterial(materialIndex));
            }

            ImportNode rootNode = null;
            for (int i = 0; i < scene.GetNumNodes(); i++)
            {
                var node = scene.GetNode(i);
                if (node.Parent == null)
                {
                    rootNode = node;
                    break;
                }
            }

            if (rootNode != null)
            {
                Node node = null;
                ConvertNode(null, null, ref node, rootNode);

                return SetNormPos(node);
                //return node;

            }
            return null;
        }

        private Node SetNormPos(Node nodee)
        {
            NodeDummy nodeDummy = new();
            nodeDummy.WorldPosition = new dvec3(0,0,0);
            nodeDummy.Name = nodee.Name;
            nodeDummy.AddChild(nodee);
            nodee.Position = new dvec3(0,0,0);
            return nodeDummy;
        }

        private void ConvertNode(Node parentNode, ImportNode parentImportNode, ref Node node, ImportNode importNode)
        {
            var transform = new mat4(importNode.Transform);
            var newTransform = new mat4(transform);
            //bool hasMesh = false;


            if (importNode.Light != null)
            {
                var importProcessor = new ImportProcessor();
                importProcessor.OutputPath = formattedPath + "/ImportModel";

                node = importer.ImportLight(importProcessor, importNode.Light);
                node.WorldTransform = importNode.Transform;
            }
            else if (importNode.Mesh is ImportMesh importMesh)
            {


                if (importMesh.HasAnimations)
                {
                    float fps = importer?.GetParameterFloat("fps") ?? 30.0f;

                    var skinnedMesh = new ObjectMeshSkinned { MeshProceduralMode = true };
                    skinnedMesh.ApplyMeshProcedural(_meshes[importMesh.Filepath]);

                    // Используем правильные методы для добавления анимации
                    if (!string.IsNullOrEmpty(importMesh.AnimationFilepath))
                    {
                        skinnedMesh.SetLayerAnimationFilePath(0, importMesh.AnimationFilepath);
                        skinnedMesh.SetLayerEnabled(0, true);
                        skinnedMesh.SetLayerWeight(0, 1.0f);
                    }

                    skinnedMesh.Speed = fps;  // Устанавливаем скорость анимации
                    
                    node = skinnedMesh;

                }

                else
                {
                    var staticMesh = new ObjectMeshStatic { MeshProceduralMode = true };
                    staticMesh.ApplyMeshProcedural(_meshes[importMesh.Filepath]);
                    node = staticMesh;
                }


                for (int i = 0; i < importMesh.GetNumGeometries(); i++)
                {
                    var geometry = importMesh.GetGeometry(i);
                    for (int s = 0; s < geometry.GetNumSurfaces(); s++)
                    {
                        var surface = geometry.GetSurface(s);
                        int surfaceIndex = surface.TargetSurface;
                        //var material = _materials[surface.Material.Filepath];
                        string materialPath = surface.Material?.Filepath;

                        if (string.IsNullOrEmpty(materialPath))
                        {
                            continue;
                        }
                        if (!_materials.TryGetValue(materialPath, out var material))
                        {
                            continue;
                        }
                        if (node is ObjectMeshStatic staticMesh)
                        {
                            staticMesh.SetMaterial(material, surfaceIndex);
                        }
                        if (node is ObjectMeshSkinned skinnedMesh)
                        {
                            skinnedMesh.SetMaterial(material, surfaceIndex);
                        }
                    }
                }

                node.WorldTransform = newTransform;
                //hasMesh = true;
            }

            else
            {
                
                node = new NodeDummy { WorldTransform = newTransform };
                
            }

            node.Name = importNode.Name;

            //_importer.ImportNodeChild(importProcessor, nodeParent, importNodeParent, node, importNode);


            for (int i = 0; i < importNode.GetNumChildren(); i++)
            {
                Node childNode = null;
                ConvertNode(node, importNode, ref childNode, importNode.GetChild(i));
                node.AddWorldChild(childNode);

            }
        }

        private bool OnProcessMesh(Mesh mesh, ImportMesh importMesh)
        {
            Guid guid = GenerateUniqueGuid();
            importMesh.Filepath = guid.ToString();

            _meshes[importMesh.Filepath] = mesh;

            return true;
        }

        private bool OnProcessMaterial(Material material, ImportMaterial importMaterial)
        {
            Guid guid = GenerateUniqueGuid();

            importMaterial.Filepath = guid.ToString();
            _materials[importMaterial.Filepath] = material;
            return true;
        }

        private bool OnProcessAnimation(MeshAnimation animation, ImportMesh importMesh, ImportAnimation importAnimation)
        {
           //string animationFilePath = $"import_animation_temp_blob_{GenerateUniqueGuid()}.anim";
            string animationFilePath = $"import_animation_temp_blob_{GenerateUniqueGuid()}.anim";
            
            bool added = FileSystem.AddBlobFile(animationFilePath);
            if (!added)
            {
                return false;
            }

            importAnimation.Filepath = animationFilePath;

            if (animation.Save(importAnimation.Filepath) == 0)
                return false;

            importMesh.AnimationFilepath = importAnimation.Filepath;
            return true;
        }
        
        // private void UV(Mesh mesh)
        // {
        //     int numSurfaces = mesh.NumSurfaces; // Получаем количество поверхностей
        //     bool UVBig = false;

        //     for (int surfaceIndex = 0; surfaceIndex < numSurfaces; surfaceIndex++)
        //     {
        //         mesh.SetSurfaceLightmapUVChannel(surfaceIndex, 0);
        //         for (int i = 0; i < mesh.GetNumTexCoords0(0); i++)
        //         {
        //             vec2 UVOld_0 = mesh.GetTexCoord0(i, surfaceIndex);
        //             if (UVOld_0.x > 1) { UVBig = true; }
        //             if (UVOld_0.y > 1) { UVBig = true; }
        //             //mesh.SetTexCoord0(i, UVOld_0 / 100.0f, surfaceIndex);
        //         }
        //     }

        //     if (UVBig)
        //     {
        //         for (int surfaceIndex = 0; surfaceIndex < numSurfaces; surfaceIndex++)
        //         {
        //             mesh.SetSurfaceLightmapUVChannel(surfaceIndex, 0);
        //             for (int i = 0; i < mesh.GetNumTexCoords0(0); i++)
        //             {
        //                 vec2 UVOld_0 = mesh.GetTexCoord0(i, surfaceIndex);
        //                 mesh.SetTexCoord0(i, UVOld_0 / 100.0f, surfaceIndex);
        //             }
        //         }
        //     }

        // }

        private Guid GenerateUniqueGuid()
        {
            Guid guid;
            do { guid = Guid.NewGuid(); } while (_guids.Contains(guid));
            _guids.Add(guid);
            return guid;
        }


    }
}