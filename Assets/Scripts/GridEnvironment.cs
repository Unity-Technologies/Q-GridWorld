using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GridEnvironment : Environment
{

    public List<GameObject> actorObjs;
    public string[] players;
    public GameObject visualAgent;
    int numObstacles;
    int numGoals;
    int gridSize;
    int[] objectPositions;
    float episodeReward;

    void Start()
    {
        maxSteps = 100;
        waitTime = 0.001f;
        BeginNewGame();
    }

    /// <summary>
    /// Restarts the learning process with a new Grid.
    /// </summary>
    public void BeginNewGame()
    {
        int gridSizeSet = (GameObject.Find("Dropdown").GetComponent<Dropdown>().value + 1) * 5;
        numGoals = 1;
        numObstacles = Mathf.FloorToInt((gridSizeSet * gridSizeSet) / 10f);
        gridSize = gridSizeSet;

        foreach (GameObject actor in actorObjs)
        {
            DestroyImmediate(actor);
        }

        SetUp();
        agent = new InternalAgent();
        agent.SendParameters(envParameters);
        Reset();

    }

    /// <summary>
    /// Established the Grid.
    /// </summary>
    public override void SetUp()
    {
        envParameters = new EnvironmentParameters()
        {
            observation_size = 0,
            state_size = gridSize * gridSize,
            action_descriptions = new List<string>() { "Up", "Down", "Left", "Right" },
            action_size = 4,
            env_name = "GridWorld",
            action_space_type = "discrete",
            state_space_type = "discrete",
            num_agents = 1
        };

        List<string> playersList = new List<string>();
        actorObjs = new List<GameObject>();
        for (int i = 0; i < numObstacles; i++)
        {
            playersList.Add("pit");
        }
        playersList.Add("agent");

        for (int i = 0; i < numGoals; i++)
        {
            playersList.Add("goal");
        }
        players = playersList.ToArray();
        Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        cam.transform.position = new Vector3((gridSize - 1), gridSize, -(gridSize - 1) / 2f);
        cam.orthographicSize = (gridSize + 5f) / 2f;
        SetEnvironment();
    }

    // Update is called once per frame
    void Update()
    {
        waitTime = 1.0f - GameObject.Find("Slider").GetComponent<Slider>().value;
        RunMdp();
    }

    /// <summary>
    /// Gets the agent's current position and transforms it into a discrete integer state.
    /// </summary>
    /// <returns>The state.</returns>
    public override List<float> collectState()
    {
        List<float> state = new List<float>();
        foreach (GameObject actor in actorObjs)
        {
            if (actor.tag == "agent")
            {
                float point = (gridSize * actor.transform.position.x) + actor.transform.position.z;
                state.Add(point);
            }
        }
        return state;
    }

    /// <summary>
    /// Resizes the grid to the specified size.
    /// </summary>
    public void SetEnvironment()
    {
        GameObject.Find("Plane").transform.localScale = new Vector3(gridSize / 10.0f, 1f, gridSize / 10.0f);
        GameObject.Find("Plane").transform.position = new Vector3((gridSize - 1) / 2f, -0.5f, (gridSize - 1) / 2f);
        GameObject.Find("sN").transform.localScale = new Vector3(1, 1, gridSize + 2);
        GameObject.Find("sS").transform.localScale = new Vector3(1, 1, gridSize + 2);
        GameObject.Find("sN").transform.position = new Vector3((gridSize - 1) / 2f, 0.0f, gridSize);
        GameObject.Find("sS").transform.position = new Vector3((gridSize - 1) / 2f, 0.0f, -1);
        GameObject.Find("sE").transform.localScale = new Vector3(1, 1, gridSize + 2);
        GameObject.Find("sW").transform.localScale = new Vector3(1, 1, gridSize + 2);
        GameObject.Find("sE").transform.position = new Vector3(gridSize, 0.0f, (gridSize - 1) / 2f);
        GameObject.Find("sW").transform.position = new Vector3(-1, 0.0f, (gridSize - 1) / 2f);

        HashSet<int> numbers = new HashSet<int>();
        while (numbers.Count < players.Length)
        {
            numbers.Add(Random.Range(0, gridSize * gridSize));
        }
        objectPositions = numbers.ToArray();
    }

    /// <summary>
    /// Draws the value estimation spheres on the grid.
    /// </summary>
    public void LoadSpheres()
    {
        GameObject[] values = GameObject.FindGameObjectsWithTag("value");
        foreach (GameObject value in values)
        {
            Destroy(value);
        }

        float[] value_estimates = agent.GetValue();
        for (int i = 0; i < gridSize * gridSize; i++)
        {
            GameObject value = (GameObject)GameObject.Instantiate(Resources.Load("value"));
            int x = i / gridSize;
            int y = i % gridSize;
            value.transform.position = new Vector3(x, 0.0f, y);
            value.transform.localScale = new Vector3(value_estimates[i] / 1.25f, value_estimates[i] / 1.25f, value_estimates[i] / 1.25f);
            if (value_estimates[i] < 0)
            {
                Material newMat = Resources.Load("negative_mat", typeof(Material)) as Material;
                value.GetComponent<Renderer>().material = newMat;
            }
        }
    }

    /// <summary>
    /// Resets the episode by placing the objects in their original positions.
    /// </summary>
    public override void Reset()
    {
        base.Reset();

        foreach (GameObject actor in actorObjs)
        {
            DestroyImmediate(actor);
        }
        actorObjs = new List<GameObject>();

        for (int i = 0; i < players.Length; i++)
        {
            int x = (objectPositions[i]) / gridSize;
            int y = (objectPositions[i]) % gridSize;
            GameObject actorObj = (GameObject)GameObject.Instantiate(Resources.Load(players[i]));
            actorObj.transform.position = new Vector3(x, 0.0f, y);
            actorObj.name = players[i];
            actorObjs.Add(actorObj);
            if (players[i] == "agent")
            {
                visualAgent = actorObj;
            }
        }
        episodeReward = 0;
        EndReset();
    }

    /// <summary>
    /// Allows the agent to take actions, and set rewards accordingly.
    /// </summary>
    /// <param name="action">Action.</param>
    public override void MiddleStep(int action)
    {
        reward = -0.05f;
        // 0 - Forward, 1 - Backward, 2 - Left, 3 - Right
        if (action == 3)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x + 1, 0, visualAgent.transform.position.z), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x + 1, 0, visualAgent.transform.position.z);
            }
        }

        if (action == 2)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x - 1, 0, visualAgent.transform.position.z), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x - 1, 0, visualAgent.transform.position.z);
            }
        }

        if (action == 0)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z + 1), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z + 1);
            }
        }

        if (action == 1)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z - 1), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z - 1);
            }
        }

        Collider[] hitObjects = Physics.OverlapBox(visualAgent.transform.position, new Vector3(0.3f, 0.3f, 0.3f));
        if (hitObjects.Where(col => col.gameObject.tag == "goal").ToArray().Length == 1)
        {
            reward = 1;
            done = true;
        }
        if (hitObjects.Where(col => col.gameObject.tag == "pit").ToArray().Length == 1)
        {
            reward = -1;
            done = true;
        }

        LoadSpheres();
        episodeReward += reward;
        GameObject.Find("RTxt").GetComponent<Text>().text = "Episode Reward: " + episodeReward.ToString("F2");

    }
}
