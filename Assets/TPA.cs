using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class TapToPlaceToggleModel : MonoBehaviour
{
    [Header("Prefabs & Assets")]
    public GameObject modelPrefab;
    public RuntimeAnimatorController animatorController;

    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    private GameObject spawnedModel;
    private bool modelPlaced = false;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        if (!modelPlaced)
        {
            // First tap: place model
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                // Instantiate model slightly below to slide up
                Vector3 startPos = hitPose.position + new Vector3(0, -0.3f, 0);
                Quaternion rotation = Quaternion.Euler(0, 180, 0); // face camera

                spawnedModel = Instantiate(modelPrefab, startPos, rotation);
                spawnedModel.transform.localScale = Vector3.one * 0.3f;
                // add collider to model so raycast could hit and register second tap. also model doesnt have collider built in.
                if (spawnedModel.GetComponentInChildren<Collider>() == null)
                {
                    Renderer meshRenderer = spawnedModel.GetComponentInChildren<Renderer>();
                    if (meshRenderer != null)
                    {
                        BoxCollider col = meshRenderer.gameObject.AddComponent<BoxCollider>();
                        col.center = meshRenderer.bounds.center - meshRenderer.transform.position;
                        col.size = meshRenderer.bounds.size;
                    }
                }


                // Add Animator dynamically
                Animator animator = spawnedModel.GetComponent<Animator>();
                if (animator == null)
                    animator = spawnedModel.AddComponent<Animator>();

                animator.runtimeAnimatorController = animatorController;

                // Start slide-in animation
                StartCoroutine(SlideUpAndAnimate(spawnedModel, hitPose.position, animator));

                modelPlaced = true;
            }
        }
        else
        {
            // Second tap: check if model was tapped
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.gameObject == spawnedModel || hit.transform.IsChildOf(spawnedModel.transform))
                {
                    // Destroy model
                    Destroy(spawnedModel);
                    modelPlaced = false;

                    // Reactivate planes
                    SetPlaneVisibility(true);
                }
            }
        }
    }

    System.Collections.IEnumerator SlideUpAndAnimate(GameObject model, Vector3 targetPos, Animator animator)
    {
        Vector3 startPos = model.transform.position;
        float duration = 0.5f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            model.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        model.transform.position = targetPos;

        // Hide all planes
        SetPlaneVisibility(false);

        // Play animation
        if (animator != null)
        {
            animator.Play("d1"); //d1 state in animator
        }
    }

    void SetPlaneVisibility(bool visible)
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(visible);
        }
    }
}
