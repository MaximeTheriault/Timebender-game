﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SpellMoveProps : MonoBehaviour
{
    int layerMaskInteractableMoveable = 1 << 10;
    int layerMaskInteractableRagdollTime = 1 << 12;
    int layerMaskDefault = 1 << 0;
    int layerIgnoreRaycast = 1 << 2;
    int layerMaskMoveable;

    private Camera camera;

    public bool drawLine = false;

    private float defaultRange;
    private float minRange = 2f;
    private float maxRange = 4.5f;
    private float breakDist = 6.5f;
    private float rayRange = 5.5f;
    private float throwForce = 25f;
    private float wandRotIntensityOnSel = 0.3f;

    public Transform holdLocation;
    private GameObject selectedProp;
    private Rigidbody selectedPropRb;
    private float defAngDrag;
    public GameObject wandLoc;
    public GameObject hitPointobj;

    private LineRenderer lineRenderer;
    public GameObject throwParticleEffect;
    private GameObject hitPointobjInstance;

    private GameManager gameManager;

    private Sway wandSway;

    public RawImage[] crosshairImages;

    public AudioSource audioSource;

    Color defColor;

    private void Awake()
    {
        wandSway = gameObject.transform.GetChild(0).GetComponent<Sway>();

        gameManager = GameManager.instance;
        camera = Camera.main;
        defaultRange = holdLocation.localPosition.z;
        layerMaskMoveable = LayerMask.NameToLayer("InteractableMoveable");
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        defColor = crosshairImages[0].color;
    }

    private void OnEnable()
    {
        StartCoroutine(CheckIfRayReach());
        gameManager.uiInteractManager.UpdateControlInfoText("LMB - THROW / RMB - GRAB / SCROLL");
    }

    private void Update()
    {
        if (!gameManager.isGamePaused)
        {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            //control selected prop, move it away or closer to player
            if (selectedProp != null)
            {
                if (Input.mouseScrollDelta.y != 0)
                {
                    if (Input.mouseScrollDelta.y > 0)
                    {
                        if (!(holdLocation.localPosition.z >= maxRange))
                        {
                            holdLocation.localPosition = new Vector3(holdLocation.localPosition.x, holdLocation.localPosition.y, holdLocation.localPosition.z + (Input.mouseScrollDelta.y * 0.1f));
                            wandSway.PullForward(wandRotIntensityOnSel + ((holdLocation.localPosition.z - defaultRange) / 15), false);
                        }
                    }
                    else
                    {
                        if (!(holdLocation.localPosition.z <= minRange))
                        {
                            holdLocation.localPosition = new Vector3(holdLocation.localPosition.x, holdLocation.localPosition.y, holdLocation.localPosition.z + (Input.mouseScrollDelta.y * 0.1f));
                            wandSway.PullForward(wandRotIntensityOnSel + ((holdLocation.localPosition.z - defaultRange) / 15), false);
                        }
                    }



                }
            }



            //move prop
            if (Input.GetMouseButtonDown(1))
            {
                if (Physics.Raycast(ray, out hit, rayRange, ~layerMaskInteractableRagdollTime & ~layerIgnoreRaycast))
                {
                    if (hit.transform.gameObject.layer == layerMaskMoveable)
                    {
                        //case where player press input, selected prop stops being selected
                        if (selectedProp == hit.transform.gameObject)
                        {
                            DropSelectedProp();
                        }
                        else
                        {
                            if (selectedProp != null)
                            {
                                DropSelectedProp();
                            }
                            else
                            {
                                //case where player press input, select hit prop, cache
                                wandSway.PullForward(wandRotIntensityOnSel, false);
                                selectedProp = hit.transform.gameObject;
                                selectedPropRb = hit.rigidbody;
                                defAngDrag = selectedPropRb.angularDrag;
                                selectedPropRb.angularDrag = 1;
                                selectedPropRb.interpolation = RigidbodyInterpolation.Interpolate;
                                selectedPropRb.useGravity = false;
                                lineRenderer.enabled = true;
                                hitPointobjInstance = Instantiate(hitPointobj, hit.point, Quaternion.identity, selectedProp.transform);
                                wandSway.intensityRotation *= -1;
                            }
                        }
                    }
                    else
                    {
                        if (selectedProp != null)
                        {
                            DropSelectedProp();
                        }
                    }

                }
                else
                {
                    //case where player press input, no layerMaskMoveableProp rayhit, selected prop stops being selected
                    if (selectedProp != null)
                    {
                        DropSelectedProp();
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                //throw
                if (Physics.Raycast(ray, out hit, rayRange, ~layerMaskInteractableRagdollTime & ~layerIgnoreRaycast))
                {

                    bool thr = false;
                    bool thrRag = false;
                    if(hit.transform.gameObject.layer == layerMaskMoveable)
                    {
                        if (selectedProp != null)
                        {
                            if (selectedProp.CompareTag("Ragdoll") && hit.transform.CompareTag("Ragdoll"))
                            {
                                //ragdoll has multiple parts, this drops the whole ragdoll when the 2 above conditions are true
                                //(ex; head is held, leg is thrown, release the head)
                                DropSelectedProp();
                                thr = true;
                                thrRag = true;
                            }
                            else if (hit.transform == selectedProp.transform)
                            {
                                //drop the selected prop before throw.
                                DropSelectedProp();
                                thr = true;
                            }
                        }
                        else
                        {
                            thr = true;
                            if (hit.transform.CompareTag("Ragdoll"))
                            {
                                thrRag = true;
                            }
                        }

                        if (thr)
                        {                           
                            float hitMass = hit.rigidbody.mass;
                            if (thrRag)
                            {
                                hitMass *= 4f;
                            }
                            wandSway.MoveForward(hitMass * 0.5f);
                            float tf;
                            audioSource.Play();
                            tf = throwForce * hitMass;
                            hit.rigidbody.AddForce(ray.direction * tf, ForceMode.Impulse);
                            GameObject temp = Instantiate(throwParticleEffect, hit.point, Quaternion.LookRotation(hit.point));
                            Destroy(temp, 2f);
                        }
                    }
                }
            }

            //line, might remove idk if I like it
            if (selectedProp != null)
            {
                if (drawLine)
                {
                    Vector3 selectedPropVector = selectedProp.transform.position;
                    Vector3 holdLocationVector = holdLocation.position;
                    Vector3 wandLocationVector = wandLoc.transform.position;
                    Vector3 rayHitPoint = hitPointobjInstance.transform.position;

                    //vector * weight of the vector on the point to be created.
                    Vector3 point1 = ((holdLocationVector * 0.6f) + (wandLocationVector * 0.4f));
                    Vector3 point2 = ((holdLocationVector * 0.7f) + (rayHitPoint * 0.15f) + (wandLocationVector * 0.15f));
                    Vector3 point3 = ((point2 * 0.5f) + (rayHitPoint * 0.5f));

                    lineRenderer.SetPosition(0, wandLocationVector);
                    lineRenderer.SetPosition(1, point1);
                    lineRenderer.SetPosition(2, point2);
                    lineRenderer.SetPosition(3, point3);
                    lineRenderer.SetPosition(4, rayHitPoint);
                }
            }
        }
  
    }


    //selected object follow holdLocation
    void FixedUpdate()
    {

        if (selectedProp != null)
        {
            //object was disabled
            if (!selectedProp.activeSelf) {
                DropSelectedProp();
            }
            else
            {
                Vector3 selectedPropVector = selectedProp.transform.position;
                Vector3 holdLocationVector = holdLocation.position;
                Vector3 wandLocationVector = wandLoc.transform.position;
                Vector3 rayHitPoint = hitPointobjInstance.transform.position;

                Vector3 dir = holdLocationVector - rayHitPoint;
                float dist = Vector3.Distance(holdLocationVector, rayHitPoint);
                if (dist > breakDist)
                {
                    DropSelectedProp();
                }
                else
                {
                    dir *= dist;
                    selectedPropRb.AddForce(dir, ForceMode.VelocityChange);
                    if (Input.GetKey(KeyCode.W))
                    {
                        selectedPropRb.velocity *= Mathf.Clamp(dist, 0.5f, 0.8f);
                    }
                    else
                    {
                        selectedPropRb.velocity *= Mathf.Clamp(dist, 0.3f, 0.6f);
                    }
                }
            }

        }
    }
    //

    private void DropSelectedProp()
    {
        selectedPropRb.interpolation = RigidbodyInterpolation.None;
        selectedPropRb.useGravity = true;
        selectedPropRb.angularDrag = defAngDrag;

        selectedProp = null;
        selectedPropRb = null;

        holdLocation.localPosition = new Vector3(0, 0, defaultRange);
        lineRenderer.enabled = false;

        wandSway.intensityRotation *= -1;
        wandSway.PullOrig();
        Destroy(hitPointobjInstance);
    }


    private void OnDisable()
    {
        StopAllCoroutines();
        gameManager.uiInteractManager.UpdateControlInfoText("");
        foreach (RawImage image in crosshairImages)
        {
            image.color = defColor;
        }

        if (selectedProp != null)
        {
            DropSelectedProp();
        }
    }

    IEnumerator CheckIfRayReach()
    {
        bool isSame = false;
        Color defColor = crosshairImages[0].color;
        //15 updates per second
        while (true)
        {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, rayRange, ~layerMaskInteractableRagdollTime & ~layerIgnoreRaycast))
            {
                if(hit.transform.gameObject.layer == layerMaskMoveable)
                {
                    if (!isSame)
                    {
                        foreach(RawImage image in crosshairImages){
                            isSame = true;
                            image.color = new Color(1f, 0.6f, 0.5f, defColor.a-0.2f);
                        }
                    }
                }
                else
                {
                    if (isSame)
                    {
                        foreach (RawImage image in crosshairImages)
                        {
                            isSame = false;
                            image.color = defColor;
                        }
                    }
                }
            }
            else
            {
                if (isSame)
                {
                    foreach (RawImage image in crosshairImages)
                    {
                        isSame = false;
                        image.color = defColor;
                    }
                }

            }
            yield return new WaitForSecondsRealtime(0.06666f);
        }
    }
}
