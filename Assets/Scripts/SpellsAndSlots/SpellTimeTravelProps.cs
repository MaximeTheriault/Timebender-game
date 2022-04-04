﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellTimeTravelProps : MonoBehaviour
{
    private Camera camera;
    private float interactRange = 3.5f;
    int layerMaskMoveable;
    int layerMaskRag;
    int layerMaskInteractableMoveable = 1 << 9;
    int layerMaskRagTime = 1 << 12;
    int layerIgnoreRaycast = 1 << 2;

    private GameManager gameManager;
    private PlayerInteractManager playerInteractManager;

    public GameObject hitPointParticlePrefab;

    // Start is called before the first frame update
    void Awake()
    {
        layerMaskMoveable = LayerMask.NameToLayer("InteractableMoveable");
        layerMaskRag = LayerMask.NameToLayer("InteractableRagdollTime");
        camera = Camera.main;
        gameManager = GameManager.instance;
        playerInteractManager = FindObjectOfType<PlayerInteractManager>();
    }


    private void OnEnable()
    {
        gameManager.isSpellTimeTravelActive = true;
        playerInteractManager.UpdateTimeText();
    }

    private void OnDisable()
    {
        gameManager.isSpellTimeTravelActive = false;
        gameManager.uiInteractManager.UpdateTimeSelectionText(-1);
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameManager.isGamePaused)
        {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            //right, to future
            if (Input.GetMouseButtonDown(1))
            {
                //another raycast when key is hit to make sure target taken is not behind anything
                if (Physics.Raycast(ray, out hit, interactRange, ~layerIgnoreRaycast))
                {
                    PutHitTo(true, hit);
                }
            }

            //left, to present
            if (Input.GetMouseButtonDown(0))
            {
                //another raycast when key is hit to make sure target taken is not behind anything
                if (Physics.Raycast(ray, out hit, interactRange, ~layerIgnoreRaycast))
                {
                    PutHitTo(false, hit);
                }
            }
        }

    }

    //-> p -> pf -> f
    private void PutHitTo(bool isRight, RaycastHit hit)
    {
        Transform objectHit = hit.transform;

        if (layerMaskMoveable == objectHit.gameObject.layer || layerMaskRag == objectHit.gameObject.layer)
        {
            //hit
            GameObject hitPointParticle = Instantiate(hitPointParticlePrefab, hit.point, Quaternion.identity);
            Destroy(hitPointParticle, 2f);

            //hit ragdoll invis box, get the script for it.
            if (layerMaskRag == objectHit.gameObject.layer)
            {
                Interactable obj = objectHit.gameObject.GetComponent<MoveRagdollTime>();
                if (isRight)
                {
                    obj.OnPress(1);
                }
                else
                {
                    obj.OnPress(-1);
                }
            }
            //hit a prop
            else
            {
                //only objects that player can put in present or future
                if (!objectHit.CompareTag("Ragdoll"))
                {
                    Interactable obj = objectHit.gameObject.GetComponent<TimeTravelReceiver>();
                    if (isRight)
                    {
                        obj.OnPress(1);
                    }
                    else
                    {
                        obj.OnPress(-1);
                    }
                }
            }
        }
    }
}