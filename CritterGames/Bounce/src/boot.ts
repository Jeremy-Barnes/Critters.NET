import 'phaser';
import Menu from './menu'
import Game from './Game'
import Snowball from './snowball';
import Score from './score';

export default class Boot extends Phaser.Scene {

    loading : Phaser.GameObjects.Text;
    loadingTicker: Phaser.Time.TimerEvent;

	preload() {
        this.loading = this.add.text(phaserGameWidth/2, phaserGameHeight/2, 'Loading', { font: '30px', fill: '#ffffff' }).setOrigin(.5);
        this.loadingTicker = this.time.addEvent({
            delay: 333,
            loop: true,
            callback: () => {
                this.loading.text = this.loading.text.length == 10 ? 'Loading' : this.loading.text + '.';
            },
            callbackScope: this
        });

        this.input.mouse.disableContextMenu();

        Snowball.preloadAssets(this);
        this.load.image('ground', 'assets/ground.png');
        this.load.image('side1', 'assets/side1.png');
        this.load.image('side2', 'assets/side2.png');
        //load imgs
	}

	create() {
        this.scene.start(Menu.SceneName);
    }
}

const phaserGameWidth : number = 600;
const phaserGameHeight: number = 600;

const config : Phaser.Types.Core.GameConfig = {
    type: Phaser.AUTO,
    backgroundColor: '#00000',
    width: phaserGameWidth,
    height: phaserGameHeight,
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
    },
    physics: {
        default: 'arcade',
        arcade: {
             gravity: { y: 700 },
            debug: true,
            debugShowBody: true,
            debugShowStaticBody: true,
            debugShowVelocity: true
        }
    },
    scene: [Boot, Menu, Game, Score]
};

const game = new Phaser.Game(config);
