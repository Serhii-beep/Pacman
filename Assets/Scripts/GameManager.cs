using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Ghost[] ghosts;
    public Pacman pacman;
    public Transform pellets;
    public bool[][] maze;
    int width = 28;
    int height = 31;
    Dictionary<Vector2, Vector2> coordMap;
    List<Vector2> aStarPath;

    public int ghostMultiplier { get; private set; } = 1;

    public int score { get; private set; }

    public int lives { get; private set; }

    private void Start()
    {
        NewGame();
    }

    private void Update()
    {
        if(lives <= 0 && Input.anyKeyDown)
        {
            NewGame();
        }
        if(Input.GetKeyDown(KeyCode.G))
        {
            foreach(Ghost ghost in ghosts)
            {
                ghost.frightened.Enable(15);
            }
            BuildPath();
        }
        if(Input.GetKeyDown(KeyCode.H))
        {
            foreach(Ghost ghost in ghosts)
            {
                ghost.frightened.Enable(15);
            }
            BuildPathGreedy();
        }
    }

    private void NewGame()
    {
        SetScore(0);
        SetLives(3);
        NewRound();
        SetMaze();
    }

    private void NewRound()
    {
        foreach(Transform pellet in pellets)
        {
            pellet.gameObject.SetActive(true);
        }
        ResetState();
    }

    private void ResetState()
    {
        ResetGhostMultiplier();
        for(int i = 0; i < ghosts.Length; ++i)
        {
            ghosts[i].ResetState();
        }

        pacman.ResetState();
    }

    private void GameOver()
    {
        for(int i = 0; i < ghosts.Length; ++i)
        {
            ghosts[i].gameObject.SetActive(false);
        }

        pacman.gameObject.SetActive(false);
    }

    private void SetScore(int score)
    {
        this.score = score;
    }

    private void SetLives(int lives)
    {
        this.lives = lives;
    }

    public void GhostEaten(Ghost ghost)
    {
        int points = ghost.points * ghostMultiplier;
        SetScore(score + points);
        ghostMultiplier++;
    }

    public void PacmanEaten()
    {
        pacman.gameObject.SetActive(false);

        SetLives(lives - 1);

        if(lives > 0)
        {
            Invoke(nameof(ResetState), 3.0f);
        }
        else
        {
            GameOver();
        }
    }

    public void PelletEaten(Pellete pellete)
    {
        pellete.gameObject.SetActive(false);
        SetScore(score + pellete.points);
        if(!HasRemainingPellets())
        {
            pacman.gameObject.SetActive(false);
            Invoke(nameof(NewRound), 3.0f);
        }
    }

    public void PowerPelletEaten(PowerPellet pellet)
    {
        for(int i = 0; i < ghosts.Length; ++i)
        {
            ghosts[i].frightened.Enable(pellet.duration);
        }

        PelletEaten(pellet);
        CancelInvoke();
        Invoke(nameof(ResetGhostMultiplier), pellet.duration);
    }

    private bool HasRemainingPellets()
    {
        foreach(Transform pellet in pellets)
        {
            if(pellet.gameObject.activeSelf)
            {
                return true;
            }
        }
        return false;
    }

    private void ResetGhostMultiplier()
    {
        ghostMultiplier = 1;
    }

    private void SetMaze()
    {
        maze = new bool[height][];
        coordMap = new Dictionary<Vector2, Vector2>();
        for(int i = 0; i < maze.Length; ++i)
        {
            maze[i] = new bool[width];
        }
        foreach(Transform pellete in pellets)
        {
            float xp = pellete.position.x + 14.0f;
            float yp = Mathf.Abs(pellete.position.y - 15.0f);
            int x = Mathf.CeilToInt(xp) - 1;
            int y = Mathf.FloorToInt(yp) - 1;
            maze[y][x] = true;
            coordMap[new Vector2(x, y)] = pellete.position;
        }
    }

    private List<Vector2> BuildPath()
    {
        var start = new AStarNode();
        start.Y = 1;
        start.X = 1;

        var finish = new AStarNode();
        finish.Y = height - 2;
        finish.X = width - 2;

        start.SetDistance(finish.X, finish.Y);

        var activeNodes = new List<AStarNode>() { start };
        var visitedNodes = new List<AStarNode>();
        while(activeNodes.Any())
        {
            var checkNode = activeNodes.OrderBy(node => node.CostDistance).First();

            if(checkNode.X == finish.X && checkNode.Y == finish.Y)
            {

                List<Vector2> path = new List<Vector2>();
                var node = checkNode;
                while(true)
                {
                    Vector2 mapped = coordMap[new Vector2(node.X, node.Y)];
                    path.Add(mapped);
                    node = node.Parent;
                    if(node == null)
                        break;
                }
                path.Reverse();
                aStarPath = path;
                StartCoroutine(MovePacman());
                return path;
            }

            visitedNodes.Add(checkNode);
            activeNodes.Remove(checkNode);

            var neighbors = GetWalkableNeighbors(checkNode, finish);

            foreach(var neighbor in neighbors)
            {
                if(visitedNodes.Any(node => node.X == neighbor.X && node.Y == neighbor.Y))
                    continue;

                if(activeNodes.Any(node => node.X == neighbor.X && node.Y == neighbor.Y))
                {
                    var existing = activeNodes.First(node => node.X == neighbor.X && node.Y == neighbor.Y);
                    if(existing.CostDistance > checkNode.CostDistance)
                    {
                        activeNodes.Remove(existing);
                        activeNodes.Add(neighbor);
                    }
                }
                else
                {
                    activeNodes.Add(neighbor);
                }
            }
        }
        return null;
    }

    private void BuildPathGreedy()
    {
        AStarNode start = new AStarNode();
        start.X = 1;
        start.Y = 1;
        AStarNode finish = new AStarNode();
        finish.X = width - 2;
        finish.Y = height - 2;

        var visited = new bool[height, width];
        visited[start.Y, start.X] = true;
        var toChect = new Queue<AStarNode>();
        var startneighbors = GetNeighbors(visited, start);
        startneighbors.ForEach(node => toChect.Enqueue(node));
        while(toChect.Count > 0)
        {
            var current = toChect.Dequeue();
            visited[current.Y, current.X] = true;
            if(current.X == finish.X && current.Y == finish.Y)
            {
                List<Vector2> path = new List<Vector2>();
                var node = current;
                while(true)
                {
                    Vector2 mapped = coordMap[new Vector2(node.X, node.Y)];
                    path.Add(mapped);
                    node = node.Parent;
                    if(node == null)
                        break;
                }
                path.Reverse();
                aStarPath = path;
                StartCoroutine(MovePacman());
                return;
            }
            var n = GetNeighbors(visited, current);
            n.ForEach(node => toChect.Enqueue(node));
        }
    }

    private List<AStarNode> GetNeighbors(bool[,] visited, AStarNode node)
    {
        List<AStarNode> result = new List<AStarNode>();
        if(node.X > 0 && !visited[node.Y, node.X - 1] && maze[node.Y][node.X - 1])
            result.Add(new AStarNode { X = node.X - 1, Y = node.Y, Parent = node });
        if(node.Y > 0 && !visited[node.Y - 1, node.X] && maze[node.Y - 1][node.X])
            result.Add(new AStarNode { X = node.X, Y = node.Y - 1, Parent = node });
        if(node.X < maze[0].Length - 1 && !visited[node.Y, node.X + 1] && maze[node.Y][node.X + 1])
            result.Add(new AStarNode { X = node.X + 1, Y = node.Y, Parent = node });
        if(node.Y < maze.Length - 1 && !visited[node.Y + 1, node.X] && maze[node.Y + 1][node.X])
            result.Add(new AStarNode { X = node.X, Y = node.Y + 1, Parent = node });
        return result;
    }

    private IEnumerator MovePacman()
    {
        WaitForSeconds wait = new WaitForSeconds(0.07f);
        pacman.movement.speedMultiplier = 0.0f;
        foreach(var vec in aStarPath)
        {
            pacman.transform.position = new Vector3(vec.x, vec.y, pacman.transform.position.z);
            yield return wait;
        }
        pacman.movement.speedMultiplier = 1.0f;
    }

    private List<AStarNode> GetWalkableNeighbors(AStarNode current, AStarNode target)
    {
        var moves = new List<AStarNode>()
        {
            new AStarNode { X = current.X, Y = current.Y - 1, Parent = current, Cost = current.Cost + 1 },
            new AStarNode { X = current.X, Y = current.Y + 1, Parent = current, Cost = current.Cost + 1 },
            new AStarNode { X = current.X - 1, Y = current.Y, Parent = current, Cost = current.Cost + 1 },
            new AStarNode { X = current.X + 1, Y = current.Y, Parent = current, Cost = current.Cost + 1 }
        };

        moves.ForEach(node => node.SetDistance(target.X, target.Y));
        return moves.Where(node => node.X >= 0 && node.X < width)
            .Where(node => node.Y >= 0 && node.Y < height)
            .Where(node => maze[node.Y][node.X] == true).ToList();
    }
}
