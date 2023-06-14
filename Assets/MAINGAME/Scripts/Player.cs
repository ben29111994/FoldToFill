using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public GameObject explodeEffect;
    GameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        var gameControllerGet = GameObject.FindGameObjectWithTag("GameController");
        gameController = gameControllerGet.GetComponent<GameController>();
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Destroy"))
        {
            gameController.isControl = true;
            //Instantiate(explodeEffect, transform.position, Quaternion.identity);
            explodeEffect.transform.parent = null;
            explodeEffect.transform.localScale = Vector3.one;
            explodeEffect.SetActive(true);
            explodeEffect.GetComponent<ParticleSystem>().Play();
            Destroy(gameObject);
        }
    }
}
