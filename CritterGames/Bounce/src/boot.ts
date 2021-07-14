import 'phaser';
import Menu from './menu'
import Game from './Game'
import Ball from './ball';
import Score from './score';

export default class Boot extends Phaser.Scene {

    loading : Phaser.GameObjects.Text;
    loadingTicker: Phaser.Time.TimerEvent;

	preload() {
        this.loading = this.add.text(phaserGameWidth/2, phaserGameHeight/2, 'Loading', { font: '30px Bubblegum Sans', fill: '#ffffff' }).setOrigin(.5);
        this.loadingTicker = this.time.addEvent({
            delay: 333,
            loop: true,
            callback: () => {
                this.loading.text = this.loading.text.length == 10 ? 'Loading' : this.loading.text + '.';
            },
            callbackScope: this
        });

        this.input.mouse.disableContextMenu();

        Ball.preloadAssets(this);
        this.load.image('bar', 'assets/bar.png');
        this.load.image('side1', 'assets/side1.png');
        this.load.audio('score1', 'assets/zap1.mp3');
        this.load.audio('score2', 'assets/zap2.mp3');

	}

	create() {
        this.scene.start(Menu.SceneName);
    }
}

const phaserGameWidth : number = 600;
const phaserGameHeight: number = 600;

const config : Phaser.Types.Core.GameConfig = {
    type: Phaser.AUTO,
    backgroundColor: '#49566b',
    width: phaserGameWidth,
    height: phaserGameHeight,
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
    },
    physics: {
        default: 'arcade',
        arcade: {
             gravity: { y: 600 },
            debug: false,
            debugShowBody: true,
            debugShowStaticBody: true,
            debugShowVelocity: true
        }
    },
    scene: [Boot, Menu, Game, Score]
};

const game = new Phaser.Game(config);
