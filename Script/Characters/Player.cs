using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[ExportGroup("组件引用")]
    [Export] public InputComponent InputComponent;
    [Export] public MoveComponent MoveComponent;
    [Export] public AttributeComponent AttributeComponent;
    [Export] public FlyComponent FlyComponent;
	[Export] public CameraComponent CameraComponent;

    [ExportGroup("地面")]
    [Export] public int 移动速度 { get; set; } = 14;
    [Export] public int 下落速度 { get; set; } = 75;
    [Export] public float 跳跃速度 { get; set; } = 4;
    [Export] public float 转身速度 { get; set; } = 0.5f;
    
    [ExportGroup("飞行")]
    [Export] public float 飞行速度 { get; set; } = 40f;       
    [Export] public float 垂直速度 { get; set; } = 40f;        
    [Export] public float 飞行加速度 { get; set; } = 5f;       
    [Export] public float 飞行减速 { get; set; } = 3f;        
    [Export] public bool 启用飞行 { get; set; } = true;       
    
    [ExportGroup("摄像机")]
    [Export] public double 摄像机灵敏度 { get; set; } = 0.003;
    [Export] public int 摄像机角度限制 { get; set; } = 40;
    
    public override void _Ready()
    {
		
	
    }

    public override void _Input(InputEvent @event)
    {
    	InputComponent.ProcessInput(@event);
    }

    public override void _PhysicsProcess(double delta)
    {
        var inputMoveDir = InputComponent.GetMoveInput();
		var inputFlyDir = InputComponent.GetFlyInput();
        if (启用飞行)
        {
            FlyComponent.PhysicsUpdate(delta, inputFlyDir, CameraComponent);
        }
        else
        {
            MoveComponent.PhysicsUpdate(delta, inputMoveDir, CameraComponent, IsOnFloor());
        }
    }

}
