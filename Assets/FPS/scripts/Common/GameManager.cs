﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//GameData, Item 클래스가 담긴 네임스페이스 명시
using DataInfo;

public class GameManager : MonoBehaviour
{
    [Header("Enemy Create Info")]
    //적 캐릭터가 출현할 위치를 담은 배열
    public Transform[] points;
    //적 캐릭터가 프리팹을 저장할 변수
    public GameObject enemy;
    //적 캐릭터를 생성할 주기
    public float createTime = 2.0f;
    //적 캐릭터의 최대 생성 주기
    public int maxEnemy = 10;
    //게임 종료 여부를 판단할 변수
    public bool isGameOver = false;

    //싱글톤에 접근하기 위한 Static 변수 선언
    public static GameManager instance = null;

    //일시 정지 여부를 판단하는 변수
    private bool isPaused;
    //Inventory의 CanvasGroup 컴포넌트를 저장할 변수
    public CanvasGroup inventoryCG;

    //주인공이 죽인 적 캐릭터의 수
    //[HideInInspector] public int killCount;
    [Header("GameData")]
    //적 캐릭터를 죽인 횟수를 표시할 텍스트 UI
    public Text killCountTxt;

    //DataManager를 저장할 변수
    private DataManager dataManager;
    public GameData gameData;

    [Header("Object Pool")]
    //생성할 총알 프리팹
    public GameObject bulletPrefab;
    //오브젝트 풀에 생성할 갯수
    public int maxPool = 10;
    public List<GameObject> bulletPool = new List<GameObject>();

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        //instance에 할당된 클래스의 인스턴스가 다를 경우 새로 생성된 클래스를 의미함
        else if(instance != this)
        {
            Destroy(this.gameObject);
        }

        //다른 씬으로 넘어가더라도 삭제되지 않고 유지함
        DontDestroyOnLoad(this.gameObject);

        //DataManager를 추출해 저장
        dataManager = GetComponent<DataManager>();
        //DataManager 초기화
        dataManager.Initialize();

        //게임의 초기 데이터 로드
        LoadGameData();

        //오브젝트 풀링 생성함수 호출
        CreatePooling();
    }

    //게임의 초기 데이터 로드
    void LoadGameData()
    {
        //DataManager를 통해 파일에 저장된 데이터 불러오기
        GameData data = dataManager.Load();

        gameData.hp = data.hp;
        gameData.damage = data.damage;
        gameData.speed = data.speed;
        gameData.killCount = data.killCount;
        gameData.equipItem = data.equipItem;

        //KILL_COUNT 키로 저장된 값을 로드
        //killCount = PlayerPrefs.GetInt("KILL_COUNT", 0);
        killCountTxt.text = "KILL " + gameData.killCount.ToString("0000");
    }

    //게임 데이터를 저장
    void SaveGameData()
    {
        dataManager.Save(gameData);
    }

    void Start()
    {
        //처음 인벤토리를 비활성화
        OnInventoryOpen(false);

        //하이러키 뷰의 SpawnPointGroup을 찾아 하위에 있는 모든 Transform 컴포넌트를 찾아옴 
        points = GameObject.Find("SpawnPointGroup").GetComponentsInChildren<Transform>();

        if(points.Length > 0)
        {
            StartCoroutine(this.CreateEnemy());
        }
    }

    //적 캐릭터를 생성하는 코루틴 함수
    IEnumerator CreateEnemy()
    {
        //게임 종료 시까지 무한 루프
        while(!isGameOver)
        {
            //현재 생성된 적 캐릭터의 개수 산출
            int enemyCount = (int)GameObject.FindGameObjectsWithTag("ENEMY").Length;

            //적 캐릭터의 최대 생성 개수보다 작을 때만 적 캐릭터를 생성
            if(enemyCount < maxEnemy)
            {
                //적 캐릭터의 생성 주기 시간만큼 대기
                yield return new WaitForSeconds(createTime);

                //불규칙적인 위치 산출
                int idx = Random.Range(1, points.Length);
                //적 캐릭터의 동적 생성
                Instantiate(enemy, points[idx].position, points[idx].rotation);
            }
            else
            {
                yield return null;
            }
        }
    }

    //오브젝트 풀에서 사용 가능한 총알을 가져오는 함수
    public GameObject GetBullet()
    {
        for(int i = 0; i < bulletPool.Count; i++)
        {
            //비활성화 여부로 사용 가능한 총알인지를 판단
            if(bulletPool[i].activeSelf == false)
            {
                return bulletPool[i];
            }
        }
        return null;
    }
    
    //오브젝트 풀에 총알을 생성하는 함수
    public void CreatePooling()
    {
        //총알을 생성해 차일드화할 페어런트 게임오브젝트를 생성
        GameObject objectPools = new GameObject("ObjectPools");

        //풀링 개수만큼 미리 총알을 생성
        for(int i = 0; i < maxPool; i++)
        {
            var obj = Instantiate<GameObject>(bulletPrefab, objectPools.transform);
            obj.name = "Bullet_" + i.ToString("00");
            //비활성화
            obj.SetActive(false);
            //리스트에 생성한 총알 추가
            bulletPool.Add(obj);
        }
    }

    //일시 정지 버튼 클릭시 호출할 함수
    public void OnPauseClick()
    {
        //일시 정지 값을 토글시킴
        isPaused = !isPaused;
        //Time Scale이 0이면 정지, 1이면 정상 속도
        Time.timeScale = (isPaused) ? 0.0f : 1.0f;
        //주인공 객체를 추출
        var playerObj = GameObject.FindGameObjectWithTag("PLAYER");
        //주인공 캐릭터에 추가된 모든 스크립트를 추출함
        var scripts = playerObj.GetComponents<MonoBehaviour>();
        //주인공의 모든 스크립트를 활성화/비활성화
        foreach(var script in scripts)
        {
            script.enabled = !isPaused;
        }

        var canvasGroup = GameObject.Find("Panel - Weapon").GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = !isPaused;
    }

    //인벤토리를 활성화/비활성화하는 함수
    public void OnInventoryOpen(bool isOpened)
    {
        inventoryCG.alpha = (isOpened) ? 1.0f : 0.0f;
        inventoryCG.interactable = isOpened;
        inventoryCG.blocksRaycasts = isOpened;
    }

    //적 캐릭터가 죽을 때마다 호출될 함수
    public void IncKillCount()
    {
        ++gameData.killCount;
        killCountTxt.text = "KILL " + gameData.killCount.ToString("0000");

        //죽인 횟수를 저장
        //PlayerPrefs.SetInt("KILL_COUNT", killCount);
    }

    void OnApplicationQuit()
    {
        //게임 종료 전 게임 데이터를 저장
        SaveGameData();
    }
}