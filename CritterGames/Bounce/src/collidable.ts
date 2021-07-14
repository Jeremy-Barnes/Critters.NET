import 'phaser';

export default interface Collidable
{
    collide(object1 : Phaser.Types.Physics.Arcade.GameObjectWithBody, object2 : Phaser.Physics.Arcade.Sprite);
    // collideWorldBounds();

}
