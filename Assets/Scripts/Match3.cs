using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class Match3 : MonoBehaviour
{
    public ArrayLayout boardLayout;

    [Header("UI Elements")]
    public Sprite[] pieces;
    public RectTransform gameBoard;
    public RectTransform killedBoard;
    public Slider slider;
    public TMP_Text textoSlider;
    public bool tiempoJuego;
    public float tiempoJuegoAutomatico;
    private float actualTiempoJuegoAutomatico;
    public GameObject panelGameOver;


    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject killedPiece;
    private int score;
    public TMP_Text scoreText;
    public TMP_Text scoreFinal;
    private int record;

    public TMP_Text textRecord;
    public TMP_Text textRecordFinal;
    public int numeroScena;
    int width = 8;
    int height = 14;
    int[] fills;
    Node[,] board;

    List<NodePiece> update;
    List<VoltearPiezas> flipped;
    List<NodePiece> dead;
    List<KilledPiece> killed;

    System.Random random;

    void Start()
    {
        StartGame();
        score = 0;
        panelGameOver.SetActive(false);
        textRecord.text = PlayerPrefs.GetInt("Record" + numeroScena, 0).ToString();
        
    }

    void Update()
    {
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for (int i = 0; i < update.Count; i++)
        {
            NodePiece piece = update[i];
            if (!piece.UpdatePiece()) finishedUpdating.Add(piece);
        }
        for (int i = 0; i < finishedUpdating.Count; i++)
        {
            NodePiece piece = finishedUpdating[i];
            VoltearPiezas flip = getFlipped(piece);
            NodePiece flippedPiece = null;

            int x = (int)piece.index.x;
            fills[x] = Mathf.Clamp(fills[x] - 1, 0, width);

            List<Point> connected = isConnected(piece.index, true);
            bool wasFlipped = (flip != null);

            if (wasFlipped) //Si volteáramos a hacer esta actualización
            {
                flippedPiece = flip.getOtherPiece(piece);
                AddPoints(ref connected, isConnected(flippedPiece.index, true));
            }

            if (connected.Count == 0) //If we didn't make a match
            {
                if (wasFlipped) //Si volteáramos
                    FlipPieces(piece.index, flippedPiece.index, false); //Flip back
            }
            else //Si hacemos un partido
            {
                foreach (Point pnt in connected) //Retire las piezas de nodo conectadas
                {
                    KillPiece(pnt);
                    Node node = getNodeAtPoint(pnt);
                    NodePiece nodePiece = node.getPiece();
                    if (nodePiece != null)
                    {
                        nodePiece.gameObject.SetActive(false);
                        dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }

                ApplyGravityToBoard();
            }

            flipped.Remove(flip); //Retire la tapa después de la actualización
            update.Remove(piece);
        }
        scoreText.text = score.ToString();
       
        if (tiempoJuego)
        {

            actualTiempoJuegoAutomatico += Time.deltaTime;
            slider.value = (tiempoJuegoAutomatico -actualTiempoJuegoAutomatico) / tiempoJuegoAutomatico;
            //aqui
            slider.value = slider.value * 100;
            textoSlider.text = slider.value.ToString("f0");
            if (actualTiempoJuegoAutomatico > tiempoJuegoAutomatico)
            {
                //ControlTotal.instancia.Saltar();
                tiempoJuegoAutomatico = 60;
                GameOver();
            }
        }
    }

    public void GameOver()
    {
        panelGameOver.SetActive(true);
        scoreFinal.text = "Has conseguido " + score.ToString()+ " puntos.";
        textRecordFinal.text = PlayerPrefs.GetInt("Record" + numeroScena, 0).ToString();
    }


    public void ApplyGravityToBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = (height - 1); y >= 0; y--) //Start at the bottom and grab the next
            {
                Point p = new Point(x, y);
                Node node = getNodeAtPoint(p);
                int val = getValueAtPoint(p);
                if (val != 0) continue; //If not a hole, move to the next
                for (int ny = (y - 1); ny >= -1; ny--)
                {
                    Point next = new Point(x, ny);
                    int nextVal = getValueAtPoint(next);
                    if (nextVal == 0)
                        continue;
                    if (nextVal != -1)
                    {
                        Node gotten = getNodeAtPoint(next);
                        NodePiece piece = gotten.getPiece();

                        //Set the hole
                        node.SetPiece(piece);
                        update.Add(piece);

                        //Make a new hole
                        gotten.SetPiece(null);
                    }
                    else//Use dead ones or create new pieces to fill holes (hit a -1) only if we choose to
                    {
                        int newVal = fillPiece();
                        NodePiece piece;
                        Point fallPnt = new Point(x, (-1 - fills[x]));
                        if(dead.Count > 0)
                        {
                            NodePiece revived = dead[0];
                            revived.gameObject.SetActive(true);
                            piece = revived;

                            dead.RemoveAt(0);
                        }
                        else
                        {
                            GameObject obj = Instantiate(nodePiece, gameBoard);
                            NodePiece n = obj.GetComponent<NodePiece>();
                            piece = n;
                        }

                        piece.Initialize(newVal, p, pieces[newVal - 1]);
                        piece.rect.anchoredPosition = getPositionFromPoint(fallPnt);

                        Node hole = getNodeAtPoint(p);
                        hole.SetPiece(piece);
                        RestablecerPieza(piece);
                        fills[x]++;
                    }
                    break;
                }
            }
        }
    }

    VoltearPiezas getFlipped(NodePiece p)
    {
        VoltearPiezas flip = null;
        for (int i = 0; i < flipped.Count; i++)
        {
            if (flipped[i].getOtherPiece(p) != null)
            {
                flip = flipped[i];
                break;
            }
        }
        return flip;
    }

    void StartGame()
    {
        fills = new int[width];
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        update = new List<NodePiece>();
        flipped = new List<VoltearPiezas>();
        dead = new List<NodePiece>();
        killed = new List<KilledPiece>();
        score = 0;
        InitializeBoard();
        VerifyBoard();
        InstantiateBoard();
    }

    void InitializeBoard()
    {
        board = new Node[width, height];
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                board[x, y] = new Node((boardLayout.rows[y].row[x]) ? - 1 : fillPiece(), new Point(x, y));
            }
        }
    }

    void VerifyBoard()
    {
        List<int> remove;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Point p = new Point(x, y);
                int val = getValueAtPoint(p);
                if (val <= 0) continue;

                remove = new List<int>();
                while (isConnected(p, true).Count > 0)
                {
                    val = getValueAtPoint(p);
                    if (!remove.Contains(val))
                        remove.Add(val);
                    setValueAtPoint(p, newValue(ref remove));
                }
            }
        }
    }

    void InstantiateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = getNodeAtPoint(new Point(x, y));

                int val = node.value;
                if (val <= 0) continue;
                GameObject p = Instantiate(nodePiece, gameBoard);
                NodePiece piece = p.GetComponent<NodePiece>();
                RectTransform rect = p.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(32 + (64 * x), -32 - (64 * y));
                piece.Initialize(val, new Point(x, y), pieces[val - 1]);
                node.SetPiece(piece);
            }
        }
    }
     
    public void RestablecerPieza(NodePiece piece)
    {
        piece.ResetPosition();
        update.Add(piece);
    }

    public void Records()
    {        
        if (score > PlayerPrefs.GetInt("Record" + numeroScena, 0))
        {
            PlayerPrefs.SetInt("Record" + numeroScena, score);
            textRecord.text = score.ToString();
        }
        
    }

    public void FlipPieces(Point one, Point two, bool main)
    {
        if (getValueAtPoint(one) < 0) return;

        Node nodeOne = getNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.getPiece();
        if (getValueAtPoint(two) > 0)
        {
            Node nodeTwo = getNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.getPiece();
            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);

            if(main)
                flipped.Add(new VoltearPiezas(pieceOne, pieceTwo));

            update.Add(pieceOne);
            update.Add(pieceTwo);
        }
        else
            RestablecerPieza(pieceOne);
    }

    void KillPiece(Point p)
    {
        List<KilledPiece> available = new List<KilledPiece>();
        for (int i = 0; i < killed.Count; i++)
            if (!killed[i].falling) available.Add(killed[i]);
        score++;
        Records();
        KilledPiece set = null;
        if (available.Count > 0)
            set = available[0];
        else
        {
            GameObject kill = GameObject.Instantiate(killedPiece, killedBoard);
            KilledPiece kPiece = kill.GetComponent<KilledPiece>();
            set = kPiece;
            killed.Add(kPiece);
            
        }

        int val = getValueAtPoint(p) - 1;
        if (set != null && val >= 0 && val < pieces.Length)
            set.Initialize(pieces[val], getPositionFromPoint(p));
    }

    List<Point> isConnected(Point p, bool main)
    {
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p);
        Point[] directions =
        {
            Point.up,
            Point.right,
            Point.down,
            Point.left
        };
        
        foreach(Point dir in directions) //Comprobando si hay 2 o más formas iguales en las direcciones
        {
            List<Point> line = new List<Point>();

            int same = 0;
            for(int i = 1; i < 3; i++)
            {
                Point check = Point.add(p, Point.mult(dir, i));
                if(getValueAtPoint(check) == val)
                {
                    line.Add(check);
                    same++; 
                }
            }

            if (same > 1) //Si hay más de 1 de la misma forma en la dirección, entonces sabemos que es una coincidencia.
                AddPoints(ref connected, line); //Agregue estos puntos a la lista global conectada
        }

        for(int i = 0; i < 2; i++) //Comprobando si estamos en medio de dos formas iguales
        {
            List<Point> line = new List<Point>();

            int same = 0;
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[i + 2]) };
            foreach (Point next in check) //Verifique ambos lados de la pieza, si son del mismo valor, agréguelos a la lista
            {
                if (getValueAtPoint(next) == val)
                {
                    line.Add(next);
                    same++;
                 
                }
            }

            if (same > 1)
                AddPoints(ref connected, line);
        }

        for(int i = 0; i < 4; i++) //Comprueba  a 2x2
        {
            List<Point> square = new List<Point>();

            int same = 0;
            int next = i + 1;
            if (next >= 4)
                next -= 4;

            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[next]), Point.add(p, Point.add(directions[i], directions[next])) };           
            foreach (Point pnt in check) //Verifique todos los lados de la pieza, si son del mismo valor, agréguelos a la lista
            {
                if (getValueAtPoint(pnt) == val)
                {
                    square.Add(pnt);                 
                    same++;
                   
                }
            }

            if (same > 2)           
                AddPoints(ref connected, square);
        
                
           
        }

        if(main) //Comprueba si hay otras coincidencias a lo largo de la coincidencia actual
        {
            for (int i = 0; i < connected.Count; i++)
                AddPoints(ref connected, isConnected(connected[i], false));
            
        }

        /* UNNESSASARY | REMOVE THIS!
        if (connected.Count > 0)
            connected.Add(p);
        */

        return connected;
    }

    void AddPoints(ref List<Point> points, List<Point> add)
    {
        foreach (Point p in add)
        {
            bool doAdd = true;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Equals(p))
                {
                    doAdd = false;
                    break;
                }
            }

            if (doAdd)
                points.Add(p);
               
            
        }

    }

    int fillPiece()
    {
        int val = 1;
        val = (random.Next(0, 100) / (100 / pieces.Length)) + 1;
        return val;
    }

    int getValueAtPoint(Point p)
    {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) return -1;
        return board[p.x, p.y].value;
    }

    void setValueAtPoint(Point p, int v)
    {
        board[p.x, p.y].value = v;
    }

    Node getNodeAtPoint(Point p)
    {
        return board[p.x, p.y];
    }

    int newValue(ref List<int> remove)
    {
        List<int> available = new List<int>();
        for (int i = 0; i < pieces.Length; i++)
            available.Add(i + 1);
        foreach (int i in remove)
            available.Remove(i);

        if (available.Count <= 0) return 0;
        return available[random.Next(0, available.Count)];
    }

    string getRandomSeed()
    {
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdeghijklmnopqrstuvwxyz1234567890!@#$%^&*()";
        for (int i = 0; i < 20; i++)
            seed += acceptableChars[Random.Range(0, acceptableChars.Length)];
        return seed;
    }

    public Vector2 getPositionFromPoint(Point p)
    {
        return new Vector2(32 + (64 * p.x), -32 - (64 * p.y));
    }
}

[System.Serializable]
public class Node
{
    public int value; //0 = blank, 1 = cube, 2 = sphere, 3 = cylinder, 4 = pryamid, 5 = diamond, -1 = hole
    public Point index;
    NodePiece piece;

    public Node(int v, Point i)
    {
        value = v;
        index = i;
    }

    public void SetPiece(NodePiece p)
    {
        piece = p;
        value = (piece == null) ? 0 : piece.value;
        if (piece == null) return;
        piece.SetIndex(index);
    }   

    public NodePiece getPiece()
    {
        return piece;
    }
}

[System.Serializable]
public class VoltearPiezas
{
    public NodePiece one;
    public NodePiece two;

    public VoltearPiezas(NodePiece o, NodePiece t)
    {
        one = o; two = t;
        
    }

    public NodePiece getOtherPiece(NodePiece p)
    {
        if (p == one)
            return two;
        else if (p == two)
            return one;
        else
            return null;
    }
}
