import 'phaser';
import Snowball from './snowball';
import Menu from './menu'

export default class Game extends Phaser.Scene {

    public static readonly SceneName: string = 'game';

    ball: Snowball;
    lives: number = 3;

    constructor() {
        super(Game.SceneName);
    }

    update(time, delta) {
    }

    public preload() {
    }

    public create() {
        this.lives = 3;
        this.input.addPointer(1);

        var side1 = this.physics.add.staticGroup();
        side1.create(0, this.game.canvas.height / 2, 'side1').setScale(1, 5).refreshBody();

        var side2 = this.physics.add.staticGroup();
        side2.create(this.game.canvas.width, this.game.canvas.height / 2, 'side2').setScale(1, 5).refreshBody();

        this.ball = new Snowball(this, this.game.canvas.width / 2, this.game.canvas.height / 3);
        this.physics.add.collider(this.ball.sprite, side1, this.ball.collide, null, this.ball);
        this.physics.add.collider(this.ball.sprite, side2, this.ball.collide, null, this.ball);

        var scoreZone = this.physics.add.image(240, 105, 'ground');
        scoreZone.setImmovable(true);
        scoreZone.setActive(true);
        (<any>scoreZone.body).allowGravity = false;
        this.physics.add.overlap(this.ball.sprite, scoreZone, this.ball.bankScore, null, this.ball);
        this.physics.world.on(Phaser.Physics.Arcade.Events.WORLD_BOUNDS, this.onBallDropped.bind(this), this);

        this.input.on(Phaser.Input.Events.POINTER_DOWN, (pointer) => {  
            this.ball.start();
            this.input.removeAllListeners(Phaser.Input.Events.POINTER_DOWN);
        }, this);

    }

    onBallDropped(world, up, down, left, right) {

        if (!down) return;
        this.lives--;
        if (this.lives <= 0) {
            this.scene.start('score', { score: this.ball.score });
        } else {
            this.ball.reset(this.game.canvas.width / 2, this.game.canvas.height / 3);
            this.input.on(Phaser.Input.Events.POINTER_DOWN, (pointer) => {  
                this.ball.start();
                this.input.removeAllListeners(Phaser.Input.Events.POINTER_DOWN);
            }, this);
        }
    }
}
