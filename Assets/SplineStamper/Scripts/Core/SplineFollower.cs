using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineFollower : MonoBehaviour
{
    Stamper stamper;
    Vector3[] evenPoints;

    [Header("Settings")]
    [Range(0, 1)] [Tooltip("Normalised target position where 0 is the first point and 1 is the last")]
    [SerializeField] float pointPosition;

    [Range(-1, 1)] [Tooltip("Left or right side along the curve")]
    [SerializeField] int side = 0;

    [Range(0, 100)]
    [SerializeField] float width = 30;[Tooltip("Distance from the point * 2")]
    Vector3 followerPosition;

    [SerializeField] [Tooltip("Will object's forward face the selected point?")]
    bool facePoint = true;

    [Header("Debug")]
    [SerializeField] bool displayGizmos = true;
    public void AdjustPosition()
    {
        Stamper stamper = GetComponentInParent<Stamper>();
        Spline spline = stamper.spline;
        evenPoints = spline.CalculateEvenlySpacedPoints(stamper.spacing, 0);

        int pointsCount = evenPoints.Length - 1;
        float currentSelected = pointsCount * pointPosition;
        Vector3 forward;
        if (currentSelected < evenPoints.Length - 1)
            forward = evenPoints[(int)currentSelected + 1] - evenPoints[(int)currentSelected];
        else forward = evenPoints[(int)currentSelected] - evenPoints[(int)currentSelected -1];

        forward.Normalize();
        Vector3 left = new Vector3(-forward.z, 0, forward.x);
        Vector3 farLeft = (evenPoints[(int)currentSelected] + (left * width * 0.5f));
        Vector3 farRight = (evenPoints[(int)currentSelected] - (left * width * 0.5f));

        float lerp = Mathf.InverseLerp(-1, 1, side);
        Vector3 current = Vector3.Lerp(farLeft, farRight, lerp);

        followerPosition = current;

        if(facePoint)
        {
            Vector3 targetDirection = (lerp < 0.5f) ? -left : left;
            Quaternion lookDirection = Quaternion.LookRotation(targetDirection, forward);
            transform.rotation = lookDirection;
        }

        transform.position = followerPosition;
    }

    private void OnValidate()
    {
        AdjustPosition();
    }
    private void OnDrawGizmos()
    {
        if(displayGizmos)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(followerPosition, 1f);
        }

    }
}
