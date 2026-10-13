using UnityEngine;

public enum SlotCheck { Empty, Correct, WrongPlace, WrongDirection }

/// <summary>
/// A spot on the drone where one picture should be dropped (for example "Front-left motor").
/// Make it an empty child object of the drone, and move it to the real place of the part.
/// </summary>
public class TrainingSlot : MonoBehaviour
{
    public PartType expectedType;
    public Spin expectedSpin = Spin.None;   // CW / CCW for motor and propeller spots
    public string slotLabel = "";           // e.g. "Front-left motor"
    public Vector2 iconOffset = Vector2.zero; // move the picture on the screen (pixels). Useful for stacked parts
    public float gizmoSize = 0.02f;         // only for the Scene view (small colored circle)

    [Header("3D look (optional)")]
    public Renderer[] realParts;            // the real meshes on the drone for this spot (shown as a ghost until the student installs the part)
    public bool ghostOwnMeshes = false;     // tick this to use all the meshes of this object and its children, no dragging needed

    public PartCard Occupant { get; private set; }

    Material[][] originals;
    bool ghostOn;
    Material ghostMat;

    public bool HasRealParts
    {
        get
        {
            FillParts();
            return realParts != null && realParts.Length > 0;
        }
    }

    void Awake()
    {
        FillParts();
    }

    // Fills realParts from this object's own meshes (done lazily, so it also works if Awake did not run yet)
    void FillParts()
    {
        if (ghostOwnMeshes && (realParts == null || realParts.Length == 0))
            realParts = GetComponentsInChildren<Renderer>(true);
    }

    bool[] originalEnabled;
    bool hideMode;

    void Capture()
    {
        if (originals != null) return;
        originals = new Material[realParts.Length][];
        originalEnabled = new bool[realParts.Length];
        for (int i = 0; i < realParts.Length; i++)
        {
            originals[i] = realParts[i] != null ? realParts[i].sharedMaterials : null;
            originalEnabled[i] = realParts[i] != null && realParts[i].enabled;
        }
    }

    /// <summary>Empty spot look: hide = the real meshes disappear completely, otherwise they become a see-through ghost.</summary>
    public void EnableGhost(Material ghost, bool hide)
    {
        if (!HasRealParts) return;
        if (!hide && ghost == null) return;

        Capture();
        ghostMat = ghost;
        hideMode = hide;
        ghostOn = true;
        ApplyGhost();
    }

    /// <summary>Stop the ghost/hide effect and bring back the original look.</summary>
    public void DisableGhost()
    {
        ghostOn = false;
        ShowSolid();
    }

    /// <summary>Original look of the real meshes (the part is "installed").</summary>
    public void ShowSolid()
    {
        if (originals == null) return;
        for (int i = 0; i < realParts.Length; i++)
        {
            if (realParts[i] == null) continue;
            if (originals[i] != null) realParts[i].sharedMaterials = originals[i];
            realParts[i].enabled = originalEnabled[i];
        }
    }

    void ApplyGhost()
    {
        if (originals == null) return;
        for (int i = 0; i < realParts.Length; i++)
        {
            if (realParts[i] == null) continue;
            if (hideMode)
            {
                realParts[i].enabled = false;
                continue;
            }
            if (originals[i] == null || ghostMat == null) continue;
            Material[] g = new Material[originals[i].Length];
            for (int k = 0; k < g.Length; k++) g[k] = ghostMat;
            realParts[i].sharedMaterials = g;
            realParts[i].enabled = originalEnabled[i];
        }
    }

    public SlotCheck Check()
    {
        if (Occupant == null || Occupant.data == null) return SlotCheck.Empty;
        if (Occupant.data.type != expectedType) return SlotCheck.WrongPlace;
        if (expectedSpin != Spin.None && Occupant.spin != expectedSpin) return SlotCheck.WrongDirection;
        return SlotCheck.Correct;
    }

    public bool IsCorrect
    {
        get { return Check() == SlotCheck.Correct; }
    }

    public void Place(PartCard card)
    {
        if (Occupant != null && Occupant != card)
            Occupant.ReturnToTray();   // kicks the old picture out (it calls Clear on this spot)

        Occupant = card;
        card.currentSlot = this;
    }

    public void Clear()
    {
        Occupant = null;
        if (ghostOn) ApplyGhost();   // spot is empty again: ghost again
    }

    /// <summary>Where this spot is on the phone screen. False if it is behind the camera.</summary>
    public bool TryGetScreenPos(Camera cam, out Vector2 pos)
    {
        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        pos = new Vector2(sp.x, sp.y) + iconOffset;
        return sp.z > 0f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = expectedSpin == Spin.CW ? Color.cyan : (expectedSpin == Spin.CCW ? Color.yellow : Color.white);
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
    }
}
