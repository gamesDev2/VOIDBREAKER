using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ButtonInteract : BaseInteractable
{
    public bool IsBeingPressed { get; private set; } = false;
    public float cooldownTime = 1f;
    private float lastPressedTime = 0f;

    private void Awake()
    {
        //set the button to be unpressed
        IsBeingPressed = false;
    }

    public override void Interact(GameObject interactor)
    {
        //if the button is not being pressed
        if (!IsBeingPressed)
        {
            //set the button to be pressed
            IsBeingPressed = true;
            lastPressedTime = Time.time;
            Debug.Log("Button pressed");
        }
    }

    private void FixedUpdate()
    {
        //if the button is being pressed
        if (IsBeingPressed)
        {
            //if the button has been pressed for more than the cooldown time
            if (Time.time - lastPressedTime > cooldownTime)
            {
                //set the button to be unpressed
                IsBeingPressed = false;
            }
        }
    }

    public override string GetInteractionPrompt()
    {
        return IsBeingPressed ? "Button is pressed" : "Press F to press";
    }
}
