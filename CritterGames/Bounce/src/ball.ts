import 'phaser';
import Collidable from './collidable';

export default class Ball implements Collidable {
    public sprite: Phaser.Physics.Arcade.Sprite;
    private ticks: number = 0;
    private lastBounceTime = 0;
    
    public wallBounces: number = 0;
    public pointerBounces: number = 0;
    public dangerBonus: number = 0;

    private phaser: Phaser.Scene;
    private danger: Phaser.Sound.BaseSound;

    constructor(phaser: Phaser.Scene, xPos, yPos) {
        this.sprite = phaser.physics.add.sprite(xPos, yPos, 'ball');
        this.sprite.setData('object', this);
        (<Phaser.Physics.Arcade.Body>this.sprite.body).onWorldBounds = true;
        this.sprite.setCollideWorldBounds(true);
        this.phaser = phaser;


        this.sprite.setBounce(.7, .5);
        this.sprite.setDamping(true);
        this.sprite.setDrag(.99, 0);
        this.stop();
        this.sprite.setTint(this.getColor(-1));
        this.danger = phaser.sound.add('danger');

    }

    public update(time: number, delta: number){
        if(this.sprite.body.allowGravity) { //handy ticking metric
            if(this.sprite.body.hitTest(this.phaser.input.activePointer.x, this.phaser.input.activePointer.y)) {
                this.pointerBounce(this.phaser.input.activePointer);
            }
            if(this.lastBounceTime > 0) {
                var bounceDiff = time - this.lastBounceTime;
                this.sprite.setTint(this.getColor(Math.round(bounceDiff/1000)));
            }
        }
    }

    public collide(collidedWith: Phaser.Types.Physics.Arcade.GameObjectWithBody, collider: Phaser.Types.Physics.Arcade.GameObjectWithBody) {        
        this.wallBounces++;
        if(this.phaser.time.now - this.lastBounceTime > 750)
            this.phaser.sound.playAudioSprite('bounce', Math.floor(Math.random() * 9 + 1).toString());
    }

    public stop() {
        (<any>this.sprite.body).allowGravity = false;
        this.sprite.body.stop();
        this.sprite.removeInteractive();
    }

    public reset(xP, yP) {
        this.stop();
        this.sprite.setPosition(xP, yP);
        this.clearStats();
    }

    public start() {
        var shape = new Phaser.Geom.Circle(
            this.sprite.width / 2,
            this.sprite.height / 2,
            this.sprite.width / 2);

        this.sprite.setInteractive(shape, Phaser.Geom.Circle.Contains);
        this.sprite.on(Phaser.Input.Events.POINTER_OVER, this.pointerBounce, this);
        (<any>this.sprite.body).allowGravity = true;
        this.clearStats();
        this.sprite.body.stop();

    }

    public clearStats(){
        this.pointerBounces = 0;
        this.wallBounces = 0;
        this.dangerBonus = 0;
        this.lastBounceTime = 0;
        this.sprite.setTint(this.getColor(-1));
    }

    private getColor(powerLevel : number){
        switch(powerLevel){
            case -1:;
            case 0: return 0xFFEC00;
            case 1: return 0xFFF35C;
            case 2: return 0xFFF9AC;
            case 3: return 0xFFFFFF;
            case 4: return 0xFFFFFF;
            case 5: return 0x33FF33;
            default: return 0xFF0000;
        }
    }

    private pointerBounce(pointer: Phaser.Input.Pointer) {
        var currentBounce = this.phaser.game.getTime();
        var bounceDiff = currentBounce - this.lastBounceTime;
        
        if(bounceDiff < 200) { //prevent double tap collision detection due to low v ball
            return;
        }

        this.sprite.setVelocityX((this.sprite.x - pointer.x) * 40);
        this.sprite.setVelocityY((this.sprite.y - pointer.y) * 18);
        this.sprite.body.velocity.add(pointer.velocity.scale(10).limit(500));
        if(Math.abs(this.sprite.body.velocity.y) > 5000){
            console.log(` Pointer v ${pointer.velocity.scale(10).y} \r\n sprite y ${this.sprite.y} \r\n pointer y ${pointer.y}`)
        }
        this.pointerBounces++;

        var dangerFactor = this.phaser.physics.world.bounds.height - pointer.y;
        if (dangerFactor < 200) {
            this.dangerBonus += 200 - Math.round(dangerFactor);
            if(dangerFactor < 50)
                this.danger.play();
        }
        if(bounceDiff != currentBounce)
            this.dangerBonus += Math.round(bounceDiff/100);
        this.lastBounceTime = currentBounce;
        this.sprite.setTint(this.getColor(-1));
        if(bounceDiff > 750)
            this.phaser.sound.playAudioSprite('bounce', Math.floor(Math.random() * 9 + 1).toString(), {volume: .5, rate:2});
    }

    static preloadAssets(phaser: Phaser.Scene) {
        phaser.load.image('ball', 'assets/ball.png');
        phaser.load.audioSprite('bounce', 'assets/bounce.json');
        phaser.load.audio('danger', 'assets/danger.mp3');

    }
}
