const City = ({city}) => {
const {name, population, area, district, photo} = city
  return (
  <tr>
    <td style={{border:'2px solid navy'}}><img src={photo}></img></td>
    <td style={{border:'2px solid navy'}}>{name}</td>
    <td style={{border:'2px solid navy'}}>{population}</td>
    <td style={{border:'2px solid navy'}}>{area}</td>
    <td style={{border:'2px solid navy'}}>{district}</td>
  </tr>
 );
}

export default City;