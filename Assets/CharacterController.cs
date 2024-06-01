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

    private int width = 25;         // �׸��� ���� ũ��
    private int height = 10;        // �׸��� ���� ũ��
    public float cellSize = 1f;     // �׸��� ���� ũ��

    public Tilemap walkableTilemap;
    public Tilemap obastacleTilemap;

    public List<Vector2> path;      // ���������� ���������� A* ���

    public bool isMoving = false;   // �÷��̾ �����̴� ���� ���콺Ŭ�� �� �� ����
    public GameObject destinationObject;    // �������� ǥ���ϱ� ���� ������Ʈ

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

            // �̵� ������ Ÿ������ Ȯ��
            if (walkableTilemap.HasTile(cellPosition) && !obastacleTilemap.HasTile(cellPosition))
            {
                // ������ ����
                destination = walkableTilemap.GetCellCenterWorld(cellPosition);
                Debug.Log("destination : " + destination);

                // ������ ǥ��
                destinationObject.gameObject.transform.position = destination;

                // A* �˰����� ����Ͽ� ��� ã��
                AStar(transform.position, destination);
            }
        }
    }

    void AStar(Vector2 start, Vector2 destination)
    {
        // ���� �ű�� F = G + H
        // F = ���� ���� (���� ���� ����, ��ο� ���� �޶���)
        // G = ���������� �ش� ��ǥ���� �̵��ϴµ� ��� ��� (���� ���� ����, ��ο� ���� �޶���)
        // H = �޸���ƽ / ���������� �󸶳� ������� (���� ���� ����, ������ �� / ���� �ְ� ������� �󸶳� ����������� ������ ����)
        bool[,] isAlreadyVisit = new bool[width, height];

        // destination���� ���� ���� �ѹ��̶� �߰��ߴ���
        // �߰� �������� max value, �ߴٸ� F �־���
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

        // �����¿�
        int[] deltaX = { 0, 0, 1, -1 };
        int[] deltaY = { 1, -1, 0, 0 };
        int[] cost = { 1, 1, 1, 1 };

        // ��� ����
        int startX = Mathf.RoundToInt(start.x);
        int startY = Mathf.RoundToInt(start.y);
        Vector2 currentPos = new Vector2(startX, startY);

        // ������ �������� ��������
        result[startX, startY] = 0 + (Math.Abs((int)destination.x - startX) + Math.Abs((int)destination.y - startY));
        pq.Push(new PQNode() { F = Math.Abs((int)destination.x - startX) + Math.Abs((int)destination.y - startY), G = 0, X = startX, Y = startY });
        parent[startX, startY] = currentPos;

        while (pq.Count > 0)
        {
            PQNode node = pq.Pop();

            // �̹� �湮�� ���� �ִ� ��� ��ŵ
            if (isAlreadyVisit[node.X, node.Y]) 
                continue;

            // �湮�Ѵ�
            isAlreadyVisit[node.X, node.Y] = true;
            
            currentPos = new Vector2(node.X, node.Y);

            // �������� ���������� ����
            if (node.X == (int)destination.x && node.Y == (int)destination.y)
            {
                break;
            }

            // �����¿� �� �̵��� �� �ִ� ���� ����
            for (int i = 0; i < deltaX.Length; i++)
            {
                int nextX = node.X + deltaX[i];
                int nextY = node.Y + deltaY[i];

                // �ε��� ���� ����� ��ŵ
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
                    continue;

                // ������ ������ �� ���� ��ŵ
                if(!walkableTilemap.HasTile(new Vector3Int(nextX, nextY, 0)))
                    continue;

                // ��ֹ� ������ �� ���� ��ŵ
                if (obastacleTilemap.HasTile(new Vector3Int(nextX, nextY, 0)))
                    continue;

                // �̹� �湮�� ���̸� ��ŵ
                if (isAlreadyVisit[nextX, nextY])
                    continue;

                // ��� ���
                int g = node.G + cost[i];
                int h = Math.Abs((int)destination.x - (int)nextX) + Math.Abs((int)destination.y - (int)nextY);

                if (result[nextX, nextY] < g + h)
                    continue;

                // ���� ����
                result[nextX, nextY] = g + h;
                pq.Push(new PQNode() { F = g + h, G = g, X = nextX, Y = nextY});
                parent[nextX, nextY] = currentPos;
            }
        }

        Debug.Log("path ����");
        CalcPathFromParent(parent, destination);
    }

    // �÷��̾ ���������� ���� ��θ� õõ�� �����ֱ� ���� �ڷ�ƾ
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

    // A* ��θ� �����ֱ� ���� �������������� ���������� ������ ��θ� ã�ư��� �Լ�
    // �� �� ��θ� �������� ���������� �������� ���� ��θ� ����
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
            if (F == other.F) // ����
                return 0;
            return F < other.F ? 1 : -1;
        }
    }
}
