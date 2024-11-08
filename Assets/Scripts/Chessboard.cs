using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.UIElements;

public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = -0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    // LOGIC
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles; // 2차원 array
    private Camera currentCamera; // 카메라
    private Vector2Int currentHover; // 마우스로 가리키고 있는 vector
    private Vector3 bounds;

    private void Awake() // 게임 실행 시 chess board 생성
    {
        transform.position = new Vector3(-3.5f, 0, -3.5f);
        GenerateAllTiles(1, TILE_COUNT_X, TILE_COUNT_Y); // 8 x 8 chess board 생성 (GenereateAllTiles 호출)
    }
    private void Update()
    {
        if (!currentCamera) // 카메라 설정 안 됐을 경우, main camera로 설정
        {
            currentCamera = Camera.main;
            return;
        }

        // tile에 마우스 올려 놓을 시(hover) 해당 tile 표시되도록 하는 과정
        RaycastHit info; // raycast가 충돌한 정보 저장
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 화면 상 좌표를 3D로 변환하는 ray 생성
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover"))) // "Tile", "Hover" 객체와 충돌하는지 검사 (충돌 시 info에 해당 tile 저장, 검사 거리 100)
        {
            // Get Indexes of the tile I've hit
            Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject); // 충돌한 객체의 index 받아서 위치정보 저장

            // If we're hovering a tile after not hovering any tiles
            if (currentHover == -Vector2Int.one) // tile 위를 hovering 하고 있지 않음 ()
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // tile에 hover 효과 적용을 위해 "Hover" layer로 변경
            }

            // If we're alreday hovering a tile, change the previous one
            if (currentHover != hitPosition) // hovering tile A -> tile B (tile 변경)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile"); // 기존 hover tile -> "Tile" layer로 복구
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // 새로운 hover tile -> "Hover" layer로 변경
            }
        }
        else // "Tile", "Hover" 객체와 충돌 X (board 아닌 곳 hover 하는 중)
        {
            if (currentHover != -Vector2Int.one) // tile -> background
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile"); // "Hover" -> "Tile"로 layer로 복구
                currentHover = -Vector2Int.one; // tile 밖이므로 위치 정보 (-1, -1) 저장
            }
        }
    }

    // Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY) // generate floor
    {
        // 주석 필요 !!
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY]; // 2차원 array 초기화
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y); // 각 tile 생성
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        // 형태 없는 tile Object 생성
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y)); // tile index
        tileObject.transform.parent = transform; // transform = Chessboard

        // tile Object에 형태 부여 (Mesh, MeshRenderer)
        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds; // 가로, 높이, 세로( - bounds 주석 필요 !!)
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 }; // 삼각형 생성 (0,1,2), (1,3,2)

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals(); // normal 자동 계산 -> 빛의 계산 이뤄져서 색 나오도록 (없으면 빛을 받지 않아서 어둡게 나옴)

        tileObject.layer = LayerMask.NameToLayer("Tile"); // 각 tile의 layer를 "Tile"로 지정
        tileObject.AddComponent<BoxCollider>(); // Collider for Raycast (각 tile의 경계선 생성)

        return tileObject; // 각 tile 반환
    }

    // Operations
    private Vector2Int LookUpTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y); // hovering tile 위치 정보 반환

        return -Vector2Int.one; // tile을 hovering 하고 있지 않음 (-1, -1)
    }
}
