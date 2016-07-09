﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.Serialization;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Examples
{
    public class VoxelmetricExample : MonoBehaviour
    {
        Vector2 rot;

        public string blockToPlace = "air";
        public Text selectedBlockText;
        public Text saveProgressText;
        public World world;

        Vector3Int pfStart;
        Vector3Int pfStop;
        public PathFinder pf;

        SaveProgress saveProgress;

        private EventSystem eventSystem;

        public void SetType(string newType){
            blockToPlace = newType;
        }

        void Start()
        {
            rot.y = 360f - transform.localEulerAngles.x;
            rot.x = transform.localEulerAngles.y;
            eventSystem = FindObjectOfType<EventSystem>();
        }

        void Update()
        {
            //Movement
            if (Input.GetMouseButton(1))
            {
                rot = new Vector2(
                    rot.x+Input.GetAxis("Mouse X")*3,
                    rot.y+Input.GetAxis("Mouse Y")*3
                    );

                transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
                transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);
            }

            bool turbo = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            transform.position += transform.forward * 40 * (turbo ? 3 : 1) * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * 40 * (turbo ? 3 : 1) * Input.GetAxis("Horizontal") * Time.deltaTime;

            //Save
            saveProgressText.text = saveProgress != null ? SaveStatus() : "Save";

            var mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

            VmRaycastHit hit = Code.Voxelmetric.Raycast(new Ray(Camera.main.transform.position, mousePos - Camera.main.transform.position), world, 1000);

            selectedBlockText.text = Code.Voxelmetric.GetBlock(world, hit.vector3Int).displayName;

            if (Input.GetMouseButtonDown(0) && !eventSystem.IsPointerOverGameObject())
            {
                if (hit.block.type != BlockProvider.AirType)
                {
                    ushort type = world.blockProvider.GetType(blockToPlace);
                    bool adjacent = type != BlockProvider.AirType;
                    Code.Voxelmetric.SetBlock(world, adjacent ? hit.adjacentPos : hit.vector3Int, new BlockData(type));
                }
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                pfStart = hit.vector3Int;
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                pfStop = hit.vector3Int;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                pf = new PathFinder(pfStart, pfStop, world, 2);
                Debug.Log(pf.path.Count);
            }

            if (pf != null && pf.path.Count != 0)
            {
                for (int i = 0; i < pf.path.Count - 1; i++)
                    Debug.DrawLine(pf.path[i].Add(0, 1, 0), pf.path[i + 1].Add(0, 1, 0));
            }
        }

        public void SaveAll()
        {
            var chunksToSave = Code.Voxelmetric.SaveAll(world);
            saveProgress = new SaveProgress(chunksToSave);
        }

        public string SaveStatus()
        {
            if (saveProgress == null)
                return "";

            return saveProgress.GetProgress() + "%";
        }

    }
}