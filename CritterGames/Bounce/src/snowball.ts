import 'phaser';
import { Tweens } from 'phaser';
import Collidable from './collidable';

export default class Snowball implements Collidable {
    public sprite: Phaser.Physics.Arcade.Sprite;
    private ticks: number = 0;
    private bounces: number = 0;
    private runningScore: number = 0;
    public score: number = 0;

    private scoreText;

    private phaser: Phaser.Scene;

    constructor(phaser: Phaser.Scene, xPos, yPos) {
        this.sprite = phaser.physics.add.sprite(xPos, yPos, 'snowball');
        this.sprite.setScale(2).refreshBody();
        this.sprite.setData('object', this);
        (<Phaser.Physics.Arcade.Body>this.sprite.body).onWorldBounds = true;
        this.sprite.setCollideWorldBounds(true);
        this.phaser = phaser;

        this.scoreText = this.phaser.add.text(25, 25, '0', { font: '30px', fill: '#ffffff' }).setOrigin(.5);

        this.sprite.setBounce(.7, .5);
        this.sprite.setDamping(true);
        this.sprite.setDrag(.99, 0);
        this.stop();
    }

    public collide(collidedWith: Phaser.Types.Physics.Arcade.GameObjectWithBody, collider: Phaser.Types.Physics.Arcade.GameObjectWithBody) {

        this.ticks = 0;
        this.phaser.time.addEvent({
            delay: 300,
            callbackScope: this,
            loop: false,
            repeat: 5,
            callback: () => {
                var c = 0xFF0000;
                switch (this.ticks) {
                    case 0: c = 0xFFEC00; break;
                    case 1: c = 0xFFF033; break;
                    case 2: c = 0xFFF35C; break;
                    case 3: c = 0xFFF57D; break;
                    case 4: c = 0xFFFABD; break;
                    default: c = 0xFFFFFF; break;
                }
                this.ticks++;
                this.sprite.setTint(c);
            }
        });
        this.bounces++;
    }

    public stop() {
        (<any>this.sprite.body).allowGravity = false;
        this.sprite.body.stop();
        this.sprite.removeInteractive();
    }

    public reset(xP, yP) {
        this.stop();
        this.sprite.setPosition(xP, yP);
    }

    public start() {
        var shape = new Phaser.Geom.Circle(
            this.sprite.width / 2,
            this.sprite.height / 2,
            this.sprite.width / 2);

        this.sprite.setInteractive(shape, Phaser.Geom.Circle.Contains);
        this.sprite.on(Phaser.Input.Events.POINTER_OVER, this.bounce, this);
        (<any>this.sprite.body).allowGravity = true;

    }

    private bounce(pointer: Phaser.Input.Pointer) {
        this.sprite.setVelocityX((this.sprite.x - pointer.x) * 40);
        this.sprite.setVelocityY((this.sprite.y - pointer.y) * 40);
        this.sprite.body.velocity.add(pointer.velocity.scale(10));

        this.runningScore += Math.pow(2, this.bounces);
        this.bounces = 0;
        var dangerFactor = this.phaser.physics.world.bounds.height - pointer.y;
        if (dangerFactor < 100) {
            this.runningScore += 100 - dangerFactor;
        }
    }

    public bankScore(ball, scoreBar) {
        if (ball.body.touching.up && scoreBar.body.touching.down && this.runningScore != 0) {
            this.score += this.runningScore;
            this.runningScore = 0;
            this.scoreText.text = this.score.toString();
        }
        return false;
    }

    static preloadAssets(phaser: Phaser.Scene) {
        phaser.load.image('snowball', 'assets/snowball.png');
    }
}
