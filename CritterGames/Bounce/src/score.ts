import Game from './game'

export default class Score extends Phaser.Scene {
    public static readonly SceneName: string = 'score';

    button: Phaser.GameObjects.Image;
    private titleText: Phaser.GameObjects.Text;
    private bodyText: Phaser.GameObjects.Text;
    private score : number = 0;
    constructor() {
        super(Score.SceneName);
    }

    init(data)
    {
        this.score = data.score;
    }

    update() {

    }

    preload() {

    }

    create() {
        this.titleText = this.add.text(this.game.canvas.width / 2, this.game.canvas.height / 3, 
            'Game over!', { font: '30px', fill: '#ffffff' }).setOrigin(.5);
        this.titleText = this.add.text(this.game.canvas.width / 2, this.game.canvas.height / 2, 
            `Final score: ${this.score}.`, { font: '20px', fill: '#ffffff' }).setOrigin(.5);

        this.input.on(Phaser.Input.Events.POINTER_DOWN, function (pointer) {  
            this.scene.start(Game.SceneName);
        }, this);
    }
}