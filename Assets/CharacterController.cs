using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class CharacterController : MonoBehaviour
{
    public Vector3 destination;
    public float movementSpeed;

    private int width = 25;         // 그리드 가로 크기
    private int height = 10;        // 그리도 세로 크기
    public float cellSize = 1f;     // 그리드 셀의 크기

    public Tilemap walkableTilemap;
    public Tilemap obastacleTilemap;

    public List<Vector2> path;      // 시작점에서 끝점까지의 A* 경로

    public bool isMoving = false;   // 플레이어가 움직이는 동안 마우스클릭 할 수 없음
    public GameObject destinationObject;    // 목적지를 표시하기 위한 오브젝트

    public bool raycastMode = false;
    
    void Start()
    {
        destination = transform.position;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            if (raycastMode)
                MouseClick_Raycast();
            else
                MouseClick_NotRaycast();
            StartCoroutine(Move());
        }
    }

    private void MouseClick_NotRaycast()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.x = Mathf.RoundToInt(worldPosition.x);
        worldPosition.y = Mathf.RoundToInt(worldPosition.y);
        Vector3Int cellPosition = walkableTilemap.WorldToCell(worldPosition);
        cellPosition.z = 0;
        Debug.Log("world pos : " + worldPosition + " cell pos: " + cellPosition);

        if (walkableTilemap.HasTile(cellPosition) && !obastacleTilemap.HasTile(cellPosition))
        {
            //destination = walkableTilemap.GetCellCenterWorld(cellPosition);
            destination = cellPosition;
            Debug.Log("destination : " + destination);

            destinationObject.gameObject.transform.position = destination;
            AStar(transform.position, destination);
        }
    }

    private void MouseClick_Raycast()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
        //Debug.DrawRay(mousePosition, Vector2.zero, Color.red, 5.0f);
        Debug.Log("mousePosition : " + mousePosition);

        if (hit.collider != null)
        {
            Vector3 worldPosition = hit.point;
            worldPosition.x = Mathf.RoundToInt(worldPosition.x);
            worldPosition.y = Mathf.RoundToInt(worldPosition.y);

            Vector3Int cellPosition = walkableTilemap.WorldToCell(worldPosition);
            cellPosition.z = 0;
            Debug.Log("world pos : " + worldPosition + " cell pos: " + cellPosition);

            // 이동 가능한 타일인지 확인
            if (walkableTilemap.HasTile(cellPosition) && !obastacleTilemap.HasTile(cellPosition))
            {
                // 목적지 설정
                destination = walkableTilemap.GetCellCenterWorld(cellPosition);
                Debug.Log("destination : " + destination);

                // 목적지 표시
                destinationObject.gameObject.transform.position = destination;

                // A* 알고리즘을 사용하여 경로 찾기
                AStar(transform.position, destination);
            }
        }
    }

    void AStar(Vector2 start, Vector2 destination)
    {
        // 점수 매기기 F = G + H
        // F = 최종 점수 (작을 수록 좋음, 경로에 따라 달라짐)
        // G = 시작점에서 해당 좌표까지 이동하는데 드는 비용 (작을 수록 좋음, 경로에 따라 달라짐)
        // H = 휴리스틱 / 목적지에서 얼마나 가까운지 (작을 수록 좋음, 고정된 값 / 벽이 있건 상관없이 얼마나 가까운지만을 생각할 거임)
        bool[,] isAlreadyVisit = new bool[width, height];

        // destination으로 가는 길을 한번이라도 발견했는지
        // 발견 못했으면 max value, 했다면 F 넣어줌
        int[,] result = new int[width, height];
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                result[i, j] = Int32.MaxValue;
            }
        }

        Vector2[,] parent = new Vector2[width, height];

        PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();

        // 상하좌우
        int[] deltaX = { 0, 0, 1, -1 };
        int[] deltaY = { 1, -1, 0, 0 };
        int[] cost = { 1, 1, 1, 1 };

        // 경로 저장
        int startX = Mathf.RoundToInt(start.x);
        int startY = Mathf.RoundToInt(start.y);
        Vector2 currentPos = new Vector2(startX, startY);

        // 시작점 기준으로 예약진행
        result[startX, startY] = 0 + (Math.Abs((int)destination.x - startX) + Math.Abs((int)destination.y - startY));
        pq.Push(new PQNode() { F = Math.Abs((int)destination.x - startX) + Math.Abs((int)destination.y - startY), G = 0, X = startX, Y = startY });
        parent[startX, startY] = currentPos;

        while (pq.Count > 0)
        {
            PQNode node = pq.Pop();

            // 이미 방문한 적이 있는 경우 스킵
            if (isAlreadyVisit[node.X, node.Y]) 
                continue;

            // 방문한다
            isAlreadyVisit[node.X, node.Y] = true;
            
            currentPos = new Vector2(node.X, node.Y);

            // 목적지에 도착했으면 종료
            if (node.X == (int)destination.x && node.Y == (int)destination.y)
            {
                break;
            }

            // 상하좌우 중 이동할 수 있는 점을 예약
            for (int i = 0; i < deltaX.Length; i++)
            {
                int nextX = node.X + deltaX[i];
                int nextY = node.Y + deltaY[i];

                // 인덱스 범위 벗어나면 스킵
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
                    continue;

                // 벽으로 막혀서 못 가면 스킵
                if(!walkableTilemap.HasTile(new Vector3Int(nextX, nextY, 0)))
                    continue;

                // 장애물 때문에 못 가면 스킵
                if (obastacleTilemap.HasTile(new Vector3Int(nextX, nextY, 0)))
                    continue;

                // 이미 방문한 곳이면 스킵
                if (isAlreadyVisit[nextX, nextY])
                    continue;

                // 비용 계산
                int g = node.G + cost[i];
                int h = Math.Abs((int)destination.x - (int)nextX) + Math.Abs((int)destination.y - (int)nextY);

                if (result[nextX, nextY] < g + h)
                    continue;

                // 예약 진행
                result[nextX, nextY] = g + h;
                pq.Push(new PQNode() { F = g + h, G = g, X = nextX, Y = nextY});
                parent[nextX, nextY] = currentPos;
            }
        }

        Debug.Log("path 생성");
        CalcPathFromParent(parent, destination);
    }

    // 플레이어가 목적지까지 가는 경로를 천천히 보여주기 위한 코루틴
    IEnumerator Move()
    {
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log(path[i]);
            while (Vector2.Distance(transform.position, path[i]) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(transform.position, path[i], movementSpeed * Time.deltaTime);
            }
            yield return new WaitForSeconds(0.5f);
        }
        path.Clear();
    }

    // A* 경로를 보여주기 위해 목적지에서부터 시작점까지 역으로 경로를 찾아가는 함수
    // 그 후 경로를 반전시켜 시작점에서 목적지로 가는 경로를 생성
    void CalcPathFromParent(Vector2[,] parent, Vector2 destination)
    {
        path.Clear();

        int x = (int)destination.x;
        int y = (int)destination.y;
        while (parent[x,y].y != y || parent[x, y].x != x)
        {
            path.Add(new Vector2(x, y));
            Vector2 pos = parent[x, y];
            x = (int)pos.x;
            y = (int)pos.y;
        }
        path.Add(new Vector2(x, y));
        path.Reverse();
    }

    struct PQNode : IComparable<PQNode>
    {
        public int F;
        public int G;
        public int X;
        public int Y;

        public int CompareTo(PQNode other)
        {
            if (F == other.F) // 동점
                return 0;
            return F < other.F ? 1 : -1;
        }
    }
}
