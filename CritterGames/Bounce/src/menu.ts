import 'phaser';
import Game from './game'

export default class Menu extends Phaser.Scene {
    button: Phaser.GameObjects.Image;
    private titleText: Phaser.GameObjects.Text;
    private bodyText: Phaser.GameObjects.Text;

    public static readonly SceneName: string = 'menu';

    constructor() {
        super(Menu.SceneName);
    }

    update() {
    }

    preload() {
    }

    create() {
        this.titleText = this.add.text(this.game.canvas.width / 2, this.game.canvas.height / 3, 'Keepy Uppy!', { font: '30px Bubblegum Sans', fill: '#ffffff' }).setOrigin(.5);
        this.bodyText = this.add.text(this.game.canvas.width / 2, this.game.canvas.height / 2, 'Tap to begin', { font: '18px Open Sans', fill: '#ffffff' }).setOrigin(.5);
        this.input.on(Phaser.Input.Events.POINTER_DOWN, function (pointer) {  
            this.scene.start(Game.SceneName);
        }, this);
    }
}