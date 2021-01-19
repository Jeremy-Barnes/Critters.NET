import 'phaser';
import Ball from './ball';

export default class Game extends Phaser.Scene {

    public static readonly SceneName: string = 'game';

    private ball: Ball;
    private lives: number = 3;
    private score: number = 0;
    private scoreText: Phaser.GameObjects.Text;
    constructor() {
        super(Game.SceneName);
    }

    update(time, delta) {
        this.ball.update(time, delta);
    }

    public preload() {
    }

    public create() {
        this.lives = 3;
        this.input.addPointer(1);
        this.score = 0;


        var side1 = this.physics.add.staticGroup();
        side1.create(0, this.game.canvas.height / 2, 'side1').setScale(1, 5).refreshBody();

        var side2 = this.physics.add.staticGroup();
        side2.create(this.game.canvas.width, this.game.canvas.height / 2, 'side1').setScale(1, 5).refreshBody();
        side1.rotate(3.1415);
        this.ball = new Ball(this, this.game.canvas.width / 2, this.game.canvas.height / 3);
        this.physics.add.collider(this.ball.sprite, side1, this.ball.collide, null, this.ball);
        this.physics.add.collider(this.ball.sprite, side2, this.ball.collide, null, this.ball);

        var scoreZone = this.physics.add.image(this.game.canvas.width/2, 85, 'bar').setOrigin(.5);
        scoreZone.setImmovable(true);
        scoreZone.setActive(true);
        (<any>scoreZone.body).allowGravity = false;
        this.physics.add.overlap(this.ball.sprite, scoreZone, this.bankScore, null, this);
        this.physics.world.on(Phaser.Physics.Arcade.Events.WORLD_BOUNDS, this.onBallDropped.bind(this), this);

        this.input.on(Phaser.Input.Events.POINTER_DOWN, (pointer) => {  
            this.ball.start();
            this.input.removeAllListeners(Phaser.Input.Events.POINTER_DOWN);
        }, this);
        this.scoreText = this.add.text(105, 25, '0', { font: '30px Bubblegum Sans', fill: '#ffffff' }).setOrigin(.5);


    }

    
    bankScore(ball, scoreBar) {
        if (ball.body.touching.up && scoreBar.body.touching.down && this.ball.pointerBounces != 0) {
            this.sound.play(`score${Math.floor(Math.random()*2+1)}`);
            this.score += this.ball.pointerBounces;
            this.score += Math.pow(2, this.ball.wallBounces);
            this.score += this.ball.dangerBonus;
            this.ball.clearStats();
            this.scoreText.text = this.score.toString();
        }
    }

    onBallDropped(world, up, down, left, right) {

        if (!down) return;
        this.lives--;
        if (this.lives <= 0) {
            this.scene.start('score', { score: this.score });
        } else {
            this.ball.reset(this.game.canvas.width / 2, this.game.canvas.height / 3);
            this.input.on(Phaser.Input.Events.POINTER_DOWN, (pointer) => {  
                this.ball.start();
                this.input.removeAllListeners(Phaser.Input.Events.POINTER_DOWN);
            }, this);
        }
    }
}
