using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;
using DG.Tweening;

public class Tile : MonoBehaviour
{
    public int ID;
    public Color tileColor;
    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    public GameObject explodeEffect;
    GameController gameController;
    bool isCheck = false;

    private void OnEnable()
    {
        var gameControllerGet = GameObject.FindGameObjectWithTag("GameController");
        gameController = gameControllerGet.GetComponent<GameController>();
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        propBlock = new MaterialPropertyBlock();

        if (meshRenderer == null)
            meshRenderer = GetComponent<Renderer>();
    }

    public void SetTransfrom(Vector3 pos,Vector3 scale)
    {
        transform.localPosition = pos;
        transform.localScale = new Vector3(scale.x,scale.y,scale.z);
    }

    public void SetColor(Color inputColor)
    {
        tileColor = inputColor;
        if (inputColor == Color.red)
        {
            gameController.transform.position = new Vector3(transform.localPosition.x, 0.6f, transform.localPosition.z);
            inputColor = Color.white;
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        }
        else if (inputColor == Color.black)
        {
            inputColor = Color.white;
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            tag = "Ground";
            gameController.progressCount++;
        }
        else
        {
            transform.position = new Vector3(transform.position.x, transform.position.y - 10, transform.position.z);
            inputColor = new Color32(50, 215, 140, 255);
            tag = "Wall";
        }
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", inputColor);
        meshRenderer.material.color = tileColor;
        meshRenderer.SetPropertyBlock(propBlock);
        gameController.instancesList.Add(GetComponent<GPUInstancerPrefab>());
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && CompareTag("Wall") && other.GetComponent<Rigidbody>().useGravity == false && !gameController.rolling)
        {
            //StartCoroutine(delayEffect1(other.gameObject));
            gameController.isControl = false;
            other.transform.parent = null;
            gameController.BoundCalculator();
            gameController.countFall++;
            other.GetComponent<Rigidbody>().useGravity = true;
        }

        if (other.CompareTag("Player") && CompareTag("Ground") && !isCheck && !gameController.rolling)
        {
            //StartCoroutine(delayEffect2(other.gameObject));
            var temp = Instantiate(explodeEffect, other.transform.position, Quaternion.identity);
            var getColor = temp.GetComponent<ParticleSystem>().main;
            getColor.startColor = gameController.theme;
            gameController.isControl = true;
            isCheck = true;
            gameController.Scoring();
        }
    }

    IEnumerator delayEffect1(GameObject other)
    {
        while(!gameController.rolling)
        {
            yield return null;
        }
        gameController.isControl = false;
        other.transform.parent = null;
        gameController.BoundCalculator();
        gameController.countFall++;
        other.GetComponent<Rigidbody>().useGravity = true;
    }

    IEnumerator delayEffect2(GameObject other)
    {
        while (!gameController.rolling)
        {
            yield return null;
        }
        gameController.isControl = true;
        isCheck = true;
        gameController.Scoring();
    }
}
