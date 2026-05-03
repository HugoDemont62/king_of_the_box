using Sandbox;

public sealed class KobPlayer : Component
{
    [RequireComponent] public CharacterController Controller { get; set; }

    [Property] public float WalkSpeed { get; set; } = 200f;
    [Property] public float JumpForce { get; set; } = 300f;
    [Property] public GameObject CameraTarget { get; set; }

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;

        HandleMovement();
        HandleCamera();
    }

    void HandleMovement()
    {
        var inputDir = Input.AnalogMove;

        var dir = Scene.Camera.WorldRotation * new Vector3( inputDir.x, inputDir.y, 0 );
        dir = dir.WithZ( 0 ).Normal;

        Controller.Accelerate( dir * WalkSpeed );
        Controller.ApplyFriction( 5f );

        if ( Controller.IsOnGround && Input.Pressed( "Jump" ) )
        {
            Controller.Punch( Vector3.Up * JumpForce );
        }

        if ( !Controller.IsOnGround )
        {
            Controller.Velocity += Vector3.Down * 800f * Time.Delta;
        }

        Controller.Move();
    }

    void HandleCamera()
    {
        if ( CameraTarget is null ) return;

        Scene.Camera.WorldPosition = CameraTarget.WorldPosition;
        Scene.Camera.WorldRotation = Rotation.From(
            Scene.Camera.WorldRotation.Pitch(),
            Input.MouseDelta.x * 0.1f + Scene.Camera.WorldRotation.Yaw(),
            0
        );
    }
}